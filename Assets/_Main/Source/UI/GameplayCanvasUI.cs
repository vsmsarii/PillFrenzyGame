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
        private float m_NextClockRefreshTime;

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

            if (Time.unscaledTime < m_NextClockRefreshTime)
                return;

            m_NextClockRefreshTime = Time.unscaledTime + 1f;
            UiText.ShowImmortal(m_Immortal, m_Save.ImmortalRemainingSeconds);
        }

        public void BindLevel(int levelNumber)
        {
            m_Level.text = "Level " + levelNumber;
        }

        public void BindPowers(SpecialPowerSystem powers, ISaveService save)
        {
            m_Save = save;
            m_PowerBar.Bind(powers);
            UiText.ShowImmortal(m_Immortal, m_Save.ImmortalRemainingSeconds);
        }

        public void BindSettings(Action settings)
        {
            m_Settings = settings;
        }

        private void OnSettingsClicked()
        {
            m_Settings.Invoke();
        }

        private void OnHudChanged(RunHudChanged evt)
        {
            UiText.SetValue(m_Score, "Score ", evt.Score);
            UiText.SetValue(m_Combo, "Combo x", evt.Combo);
            UiText.SetValue(m_Health, "HP ", evt.Health);
        }

        private void OnFillChanged(RunTargetFillChanged evt)
        {
            m_FillBuilder.Clear();
            for (int i = 0; i < evt.Fills.Length; i++)
            {
                TargetFill fill = evt.Fills[i];
                if (i > 0)
                    m_FillBuilder.Append("   ");

                m_FillBuilder.Append(fill.Color.DisplayName).Append(' ').Append(fill.Occupied).Append('/').Append(fill.Capacity);
            }

            if (evt.Remaining > 0)
                m_FillBuilder.Append("   +").Append(evt.Remaining);

            m_Fill.SetText(m_FillBuilder);
        }
    }
}
