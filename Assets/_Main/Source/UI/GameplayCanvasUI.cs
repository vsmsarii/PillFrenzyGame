using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class GameplayCanvasUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Level;
        [SerializeField] private TMP_Text m_Score;
        [SerializeField] private TMP_Text m_Combo;
        [SerializeField] private TMP_Text m_Health;
        [SerializeField] private TMP_Text m_Fill;
        [SerializeField] private TMP_Text m_Immortal;
        [SerializeField] private Button m_SettingsButton;
        [SerializeField] private SpecialPowerBarUI m_PowerBar;

        private readonly System.Text.StringBuilder m_FillBuilder = new System.Text.StringBuilder(64);

        private ISaveService m_Save;
        private System.Action m_Settings;
        private long m_ShownImmortalSeconds = long.MinValue;
        private bool m_ShownImmortalActive;

        private void Awake()
        {
            if (m_SettingsButton != null)
                m_SettingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnEnable()
        {
            EB.Presentation.Add<RunHudChanged>(OnHudChanged);
            EB.Presentation.Add<RunTargetFillChanged>(OnFillChanged);
        }

        private void OnDisable()
        {
            EB.Presentation.Remove<RunHudChanged>(OnHudChanged);
            EB.Presentation.Remove<RunTargetFillChanged>(OnFillChanged);
        }

        private void Update()
        {
            RefreshImmortal();
        }

        public void BindLevel(int levelNumber)
        {
            if (m_Level == null)
                return;

            m_Level.text = "Level " + levelNumber;
        }

        public void BindPowers(SpecialPowerSystem powers, ISaveService save)
        {
            m_Save = save;
            m_ShownImmortalSeconds = long.MinValue;
            m_ShownImmortalActive = false;
            if (m_PowerBar == null)
                return;

            m_PowerBar.Bind(powers, save);
            RefreshImmortal();
        }

        public void BindSettings(System.Action settings)
        {
            m_Settings = settings;
            if (m_SettingsButton == null)
                return;

            m_SettingsButton.gameObject.SetActive(settings != null);
        }

        private void OnSettingsClicked()
        {
            if (m_Settings != null)
                m_Settings.Invoke();
        }

        private void RefreshImmortal()
        {
            if (m_Immortal == null || m_Save == null)
                return;

            long remaining = m_Save.ImmortalRemainingSeconds;
            bool active = remaining > 0;
            if (!active)
            {
                if (m_ShownImmortalActive)
                {
                    m_ShownImmortalActive = false;
                    m_ShownImmortalSeconds = long.MinValue;
                    m_Immortal.gameObject.SetActive(false);
                }

                return;
            }

            if (!m_ShownImmortalActive)
            {
                m_ShownImmortalActive = true;
                m_Immortal.gameObject.SetActive(true);
            }

            if (remaining == m_ShownImmortalSeconds)
                return;

            m_ShownImmortalSeconds = remaining;
            long minutes = remaining / 60;
            long seconds = remaining % 60;
            m_Immortal.text = "Immortal " + minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        private void OnHudChanged(RunHudChanged evt)
        {
            if (m_Score != null)
                m_Score.text = "Score " + evt.Score;
            if (m_Combo != null)
                m_Combo.text = "Combo x" + evt.Combo;
            if (m_Health != null)
                m_Health.text = "HP " + evt.Health;
        }

        private void OnFillChanged(RunTargetFillChanged evt)
        {
            if (m_Fill == null)
                return;

            if (evt.Fills == null || evt.Fills.Length == 0)
            {
                m_Fill.text = string.Empty;
                return;
            }

            m_FillBuilder.Clear();
            for (int i = 0; i < evt.Fills.Length; i++)
            {
                TargetFill fill = evt.Fills[i];
                if (i > 0)
                    m_FillBuilder.Append("   ");

                m_FillBuilder.Append(fill.Color);
                m_FillBuilder.Append(' ');
                m_FillBuilder.Append(fill.Occupied);
                m_FillBuilder.Append('/');
                m_FillBuilder.Append(fill.Capacity);
            }

            m_Fill.text = m_FillBuilder.ToString();
        }
    }
}
