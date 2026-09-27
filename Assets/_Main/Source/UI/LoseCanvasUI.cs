using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class LoseCanvasUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Detail;
        [SerializeField] private Button m_RetryButton;
        [SerializeField] private Button m_MenuButton;

        private Action m_Retry;
        private Action m_Menu;

        private void Awake()
        {
            m_RetryButton.onClick.AddListener(OnRetryClicked);
            m_MenuButton.onClick.AddListener(OnMenuClicked);
        }

        public void Show(int score, int bestCombo, Action retry, Action menu)
        {
            m_Retry = retry;
            m_Menu = menu;
            m_Detail.text = "Score " + score + "   Best combo x" + bestCombo;
            m_RetryButton.interactable = retry != null;
            m_MenuButton.interactable = true;
        }

        private void OnRetryClicked()
        {
            DisableButtons();
            m_Retry.Invoke();
        }

        private void OnMenuClicked()
        {
            DisableButtons();
            m_Menu.Invoke();
        }

        private void DisableButtons()
        {
            m_RetryButton.interactable = false;
            m_MenuButton.interactable = false;
        }
    }
}
