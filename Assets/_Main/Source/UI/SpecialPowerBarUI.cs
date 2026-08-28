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
        private bool m_ShownVisible;
        private bool m_HasShownVisible;

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
            m_HasShownVisible = false;
            Build();
            Refresh();
        }

        private void Build()
        {
            if (m_ButtonRoot == null || m_System == null || m_System.Catalog == null)
                return;

            if (m_ButtonPrefab == null)
                return;

            SpecialPowerCatalogEntry[] entries = m_System.Catalog.Entries;
            if (entries == null)
                return;

            ClearButtons();
            m_Buttons = new SpecialPowerButtonView[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                SpecialPowerCatalogEntry entry = entries[i];
                if (entry.Definition == null)
                    continue;

                SpecialPowerButtonView button = Instantiate(m_ButtonPrefab, m_ButtonRoot);
                ESpecialPowerId id = entry.Definition.Id;
                button.Bind(entry.Definition, () => OnPowerClicked(id));
                m_Buttons[i] = button;
            }
        }

        private void ClearButtons()
        {
            if (m_Buttons != null)
            {
                for (int i = 0; i < m_Buttons.Length; i++)
                {
                    if (m_Buttons[i] != null)
                        Destroy(m_Buttons[i].gameObject);
                }
            }

            m_Buttons = null;
            if (m_ButtonRoot == null)
                return;

            for (int i = m_ButtonRoot.childCount - 1; i >= 0; i--)
                Destroy(m_ButtonRoot.GetChild(i).gameObject);
        }

        private void OnPowerClicked(ESpecialPowerId id)
        {
            if (m_System == null)
                return;

            m_System.TryActivate(id);
            Refresh();
        }

        private void OnHudChanged(SpecialPowerHudChanged _)
        {
            Refresh();
        }

        private void Refresh()
        {
            int levelNumber = m_Save != null ? m_Save.CurrentLevelNumber : 1;
            bool visible = m_System != null && m_System.Catalog != null && m_System.Catalog.HasAnyUnlocked(levelNumber);

            if (!m_HasShownVisible || visible != m_ShownVisible)
            {
                m_HasShownVisible = true;
                m_ShownVisible = visible;
                if (m_Root != null)
                    m_Root.gameObject.SetActive(visible);
                else
                    gameObject.SetActive(visible);
            }

            if (!visible || m_Buttons == null || m_System == null)
                return;

            SpecialPowerCatalogEntry[] entries = m_System.Catalog.Entries;
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                SpecialPowerButtonView button = m_Buttons[i];
                if (button == null || entries == null || i >= entries.Length || entries[i].Definition == null)
                    continue;

                ESpecialPowerId id = entries[i].Definition.Id;
                bool unlocked = m_System.IsUnlocked(id);
                button.gameObject.SetActive(unlocked);
                if (!unlocked)
                    continue;

                button.SetState(
                    m_System.GetCharges(id),
                    m_System.IsActive(id),
                    m_System.GetActiveRemaining(id));
            }
        }
    }
}
