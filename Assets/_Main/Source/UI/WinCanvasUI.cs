using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class WinCanvasUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Title;
        [SerializeField] private TMP_Text m_Detail;
        [SerializeField] private Button m_RetryButton;
        [SerializeField] private Button m_NextButton;

        private Action m_Continue;
        private bool m_ContinueRequested;

        private void Awake()
        {
            if (m_RetryButton != null)
                m_RetryButton.gameObject.SetActive(false);

            m_NextButton.onClick.AddListener(OnContinueClicked);
        }

        public void Show(int score, int bestCombo, Action continueAction)
        {
            m_Continue = continueAction;
            m_ContinueRequested = false;
            m_Title.text = "COMPLETE";
            m_Detail.text = "Score " + score + "   Best combo x" + bestCombo;
            m_NextButton.gameObject.SetActive(true);
            m_NextButton.interactable = true;
            m_NextButton.GetComponentInChildren<TMP_Text>().text = "CONTINUE";
        }

        private void OnContinueClicked()
        {
            if (m_ContinueRequested)
                return;

            m_ContinueRequested = true;
            m_NextButton.interactable = false;
            m_Continue.Invoke();
        }
    }
}
