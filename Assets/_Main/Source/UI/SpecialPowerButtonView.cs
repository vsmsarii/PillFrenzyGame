using System;
using PillFrenzy.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class SpecialPowerButtonView : MonoBehaviour
    {
        [SerializeField] private Button m_Button;
        [SerializeField] private Image m_Icon;
        [SerializeField] private TMP_Text m_Charges;
        [SerializeField] private TMP_Text m_Timer;

        private Action m_Click;
        private int m_ShownCharges = -1;
        private int m_ShownTimerTenths = -2;

        private void Awake()
        {
            m_Button.onClick.AddListener(OnClicked);
        }

        public void Bind(SpecialPowerDefinitionSO definition, Action click)
        {
            m_Click = click;
            if (definition.Icon != null)
                m_Icon.sprite = definition.Icon;
        }

        public void SetState(int charges, bool active)
        {
            m_Button.interactable = !active && charges > 0;

            if (charges != m_ShownCharges)
            {
                m_ShownCharges = charges;
                UiText.SetValue(m_Charges, string.Empty, charges);
            }
        }

        public void SetTimer(float remaining)
        {
            int tenths = remaining > 0f ? Mathf.RoundToInt(remaining * 10f) : -1;
            if (tenths == m_ShownTimerTenths)
                return;

            m_ShownTimerTenths = tenths;
            if (tenths < 0)
            {
                m_Timer.text = string.Empty;
                return;
            }

            UiText.Begin();
            UiText.Append(tenths / 10);
            UiText.Append('.');
            UiText.Append(tenths % 10);
            UiText.Append('s');
            UiText.ApplyTo(m_Timer);
        }

        private void OnClicked()
        {
            m_Click.Invoke();
        }
    }
}
