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

        public void Bind(SpecialPowerDefinitionSO definition, Action click)
        {
            m_Click = click;
            m_ShownCharges = -1;
            m_ShownTimerTenths = -2;

            m_Button.onClick.RemoveListener(OnClicked);
            m_Button.onClick.AddListener(OnClicked);

            if (definition.Icon != null)
                m_Icon.sprite = definition.Icon;
        }

        public void SetState(int charges, bool active, float remaining)
        {
            m_Button.interactable = !active && charges > 0;

            if (charges != m_ShownCharges)
            {
                m_ShownCharges = charges;
                m_Charges.text = charges.ToString();
            }

            int tenths = active && remaining > 0f ? Mathf.RoundToInt(remaining * 10f) : -1;
            if (tenths == m_ShownTimerTenths)
                return;

            m_ShownTimerTenths = tenths;
            m_Timer.text = tenths < 0 ? string.Empty : (tenths / 10f).ToString("0.0") + "s";
        }

        private void OnClicked()
        {
            m_Click.Invoke();
        }
    }
}
