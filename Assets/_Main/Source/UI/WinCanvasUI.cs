using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class WinCanvasUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Detail;
        [SerializeField] private Button m_NextButton;

        private Action m_Continue;

        private void Awake()
        {
            m_NextButton.onClick.AddListener(OnContinueClicked);
        }

        public void Show(int score, int bestCombo, Action continueAction)
        {
            m_Continue = continueAction;
            m_Detail.text = "Score " + score + "   Best combo x" + bestCombo;
            m_NextButton.interactable = true;
        }

        private void OnContinueClicked()
        {
            m_NextButton.interactable = false;
            m_Continue.Invoke();
        }
    }
}
