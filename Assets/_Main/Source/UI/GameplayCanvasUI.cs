using System;
using System.Text;
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

        private readonly StringBuilder m_FillBuilder = new StringBuilder(64);

        private ISaveService m_Save;
        private Action m_Settings;
        private long m_LastRefreshSecond = -1;

        private void Awake()
        {
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
            if (m_Save == null)
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now == m_LastRefreshSecond)
                return;

            m_LastRefreshSecond = now;
            UiText.ShowImmortal(m_Immortal, m_Save.ImmortalRemainingSeconds);
        }

        public void BindLevel(int levelNumber)
        {
            m_Level.text = "Level " + levelNumber;
        }

        public void BindPowers(SpecialPowerSystem powers, ISaveService save)
        {
            m_Save = save;
            m_PowerBar.Bind(powers, save);
            UiText.ShowImmortal(m_Immortal, m_Save.ImmortalRemainingSeconds);
        }

        public void BindSettings(Action settings)
        {
            m_Settings = settings;
            m_SettingsButton.gameObject.SetActive(settings != null);
        }

        private void OnSettingsClicked()
        {
            m_Settings?.Invoke();
        }

        private void OnHudChanged(RunHudChanged evt)
        {
            m_Score.text = "Score " + evt.Score;
            m_Combo.text = "Combo x" + evt.Combo;
            m_Health.text = "HP " + evt.Health;
        }

        private void OnFillChanged(RunTargetFillChanged evt)
        {
            m_FillBuilder.Clear();
            for (int i = 0; i < evt.Fills.Length; i++)
            {
                TargetFill fill = evt.Fills[i];
                if (i > 0)
                    m_FillBuilder.Append("   ");

                m_FillBuilder.Append(fill.Color.name).Append(' ').Append(fill.Occupied).Append('/').Append(fill.Capacity);
            }

            if (evt.Remaining > 0)
                m_FillBuilder.Append("   +").Append(evt.Remaining);

            m_Fill.text = m_FillBuilder.ToString();
        }
    }
}
