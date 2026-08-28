using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PillFrenzy.Gameplay;

namespace PillFrenzy.UI
{
    public sealed class SpecialPowerButtonView : MonoBehaviour
    {
        [SerializeField] private Button m_Button;
        [SerializeField] private Image m_Icon;
        [SerializeField] private TMP_Text m_Charges;
        [SerializeField] private TMP_Text m_Timer;

        private Action m_Click;
        private int m_ShownCharges = int.MinValue;
        private int m_ShownTimerTenths = int.MinValue;
        private bool m_ShownInteractable = true;
        private bool m_HasShownInteractable;

        public void Bind(SpecialPowerDefinitionSO definition, Action click)
        {
            m_Click = click;
            m_ShownCharges = int.MinValue;
            m_ShownTimerTenths = int.MinValue;
            m_HasShownInteractable = false;

            if (m_Button != null)
            {
                m_Button.onClick.RemoveListener(OnClicked);
                m_Button.onClick.AddListener(OnClicked);
            }

            if (m_Icon != null && definition != null && definition.Icon != null)
                m_Icon.sprite = definition.Icon;
        }

        public void SetState(int charges, bool active, float remaining)
        {
            if (m_Charges != null && charges != m_ShownCharges)
            {
                m_ShownCharges = charges;
                m_Charges.text = charges.ToString();
            }

            if (m_Timer != null)
            {
                int tenths = active && remaining > 0f ? Mathf.RoundToInt(remaining * 10f) : -1;
                if (tenths != m_ShownTimerTenths)
                {
                    m_ShownTimerTenths = tenths;
                    if (tenths < 0)
                        m_Timer.text = string.Empty;
                    else
                        m_Timer.text = (tenths / 10f).ToString("0.0") + "s";
                }
            }

            if (m_Button != null)
            {
                bool interactable = !active && charges > 0;
                if (!m_HasShownInteractable || interactable != m_ShownInteractable)
                {
                    m_HasShownInteractable = true;
                    m_ShownInteractable = interactable;
                    m_Button.interactable = interactable;
                }
            }
        }

        private void OnClicked()
        {
            if (m_Click != null)
                m_Click.Invoke();
        }
    }
}
