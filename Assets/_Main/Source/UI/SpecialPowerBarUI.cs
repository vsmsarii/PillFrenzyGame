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
        private SpecialPowerButtonView[] m_Buttons;
        private ESpecialPowerId[] m_ButtonIds;

        private void Awake()
        {
            EB.Presentation.Add<SpecialPowerHudChanged>(OnHudChanged);
        }

        private void OnDestroy()
        {
            EB.Presentation.Remove<SpecialPowerHudChanged>(OnHudChanged);
        }

        private void Update()
        {
            if (m_System != null && m_System.IsAnyActive)
                RefreshTimers();
        }

        public void Bind(SpecialPowerSystem system)
        {
            m_System = system;
            Build();
            Refresh();
        }

        private void Build()
        {
            for (int i = m_ButtonRoot.childCount - 1; i >= 0; i--)
                Destroy(m_ButtonRoot.GetChild(i).gameObject);

            SpecialPowerCatalogEntry[] entries = m_System.Catalog.Entries;
            m_Buttons = new SpecialPowerButtonView[entries.Length];
            m_ButtonIds = new ESpecialPowerId[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                SpecialPowerDefinitionSO definition = entries[i].Definition;
                if (definition == null)
                    continue;

                SpecialPowerButtonView button = Instantiate(m_ButtonPrefab, m_ButtonRoot);
                ESpecialPowerId id = definition.Id;
                button.Bind(definition, () => m_System.TryActivate(id));
                m_Buttons[i] = button;
                m_ButtonIds[i] = id;
            }
        }

        private void OnHudChanged(SpecialPowerHudChanged _)
        {
            Refresh();
        }

        private void Refresh()
        {
            bool visible = m_System != null && m_System.HasAnyRevealed;
            m_Root.gameObject.SetActive(visible);
            if (!visible || m_Buttons == null)
                return;

            for (int i = 0; i < m_Buttons.Length; i++)
            {
                SpecialPowerButtonView button = m_Buttons[i];
                if (button == null)
                    continue;

                ESpecialPowerId id = m_ButtonIds[i];
                bool revealed = m_System.IsRevealed(id);
                button.gameObject.SetActive(revealed);
                if (!revealed)
                    continue;

                button.SetState(m_System.GetCharges(id), m_System.IsActive(id));
                button.SetTimer(m_System.GetActiveRemaining(id));
            }
        }

        private void RefreshTimers()
        {
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                if (m_Buttons[i] != null)
                    m_Buttons[i].SetTimer(m_System.GetActiveRemaining(m_ButtonIds[i]));
            }
        }
    }
}
