using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class LoseCanvasUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Title;
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

            m_Title.text = "FAIL";
            m_Detail.text = "Score " + score + "   Best combo x" + bestCombo;
            m_RetryButton.interactable = retry != null;
            m_RetryButton.GetComponentInChildren<TMP_Text>().text = "Retry";
            m_MenuButton.gameObject.SetActive(true);
        }

        private void OnRetryClicked()
        {
            Action retry = m_Retry;
            m_Retry = null;
            retry?.Invoke();
        }

        private void OnMenuClicked()
        {
            Action menu = m_Menu;
            m_Menu = null;
            menu?.Invoke();
        }
    }
}
