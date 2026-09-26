using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using UnityEngine;

namespace PillFrenzy.UI
{
    public sealed class SpecialPowerBarUI : MonoBehaviour
    {
        [SerializeField] private Transform m_Root;
        [SerializeField] private Transform m_ButtonRoot;
        [SerializeField] private SpecialPowerButtonView m_ButtonPrefab;

        private SpecialPowerSystem m_System;
        private ISaveService m_Save;
        private SpecialPowerButtonView[] m_Buttons;

        private void OnEnable()
        {
            EB.Presentation.Add<SpecialPowerHudChanged>(OnHudChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EB.Presentation.Remove<SpecialPowerHudChanged>(OnHudChanged);
        }

        private void Update()
        {
            if (m_System != null && m_System.IsAnyActive)
                Refresh();
        }

        public void Bind(SpecialPowerSystem system, ISaveService save)
        {
            m_System = system;
            m_Save = save;
            Build();
            Refresh();
        }

        private void Build()
        {
            for (int i = m_ButtonRoot.childCount - 1; i >= 0; i--)
                Destroy(m_ButtonRoot.GetChild(i).gameObject);

            m_Buttons = null;
            if (m_System.Catalog == null || m_System.Catalog.Entries == null)
                return;

            SpecialPowerCatalogEntry[] entries = m_System.Catalog.Entries;
            m_Buttons = new SpecialPowerButtonView[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                SpecialPowerDefinitionSO definition = entries[i].Definition;
                if (definition == null)
                    continue;

                SpecialPowerButtonView button = Instantiate(m_ButtonPrefab, m_ButtonRoot);
                ESpecialPowerId id = definition.Id;
                button.Bind(definition, () => OnPowerClicked(id));
                m_Buttons[i] = button;
            }
        }

        private void OnPowerClicked(ESpecialPowerId id)
        {
            m_System.TryActivate(id);
            Refresh();
        }

        private void OnHudChanged(SpecialPowerHudChanged _)
        {
            Refresh();
        }

        private void Refresh()
        {
            bool visible = m_System != null
                && m_System.Catalog != null && m_System.Catalog.HasAnyUnlocked(m_Save.CurrentLevelNumber);
            m_Root.gameObject.SetActive(visible);
            if (!visible || m_Buttons == null)
                return;

            SpecialPowerCatalogEntry[] entries = m_System.Catalog.Entries;
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                SpecialPowerButtonView button = m_Buttons[i];
                if (button == null)
                    continue;

                ESpecialPowerId id = entries[i].Definition.Id;
                bool unlocked = m_System.IsUnlocked(id);
                button.gameObject.SetActive(unlocked);
                if (unlocked)
                    button.SetState(m_System.GetCharges(id), m_System.IsActive(id), m_System.GetActiveRemaining(id));
            }
        }
    }
}
