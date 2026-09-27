using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PillFrenzy.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class TutorialCanvasUI : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private TMP_Text m_Title;
        [SerializeField] private TMP_Text m_Body;
        [SerializeField] private Image m_Image;
        [SerializeField] private TMP_Text m_PageCounter;

        [Header("Buttons")]
        [SerializeField] private Button m_NextButton;
        [SerializeField] private TMP_Text m_NextLabel;
        [SerializeField] private Button m_SkipButton;

        [Header("Labels")]
        [SerializeField] private string m_NextText = "NEXT";
        [SerializeField] private string m_DoneText = "PLAY";

        private IReadOnlyList<TutorialPage> m_Pages;
        private Action<int> m_OnPageShown;
        private UniTaskCompletionSource<TutorialPresentResult> m_Completion;
        private int m_PageIndex;
        private int m_PagesViewed;
        private string m_CurrentDoneText;

        private void Awake()
        {
            m_NextButton.onClick.AddListener(OnNextClicked);
            m_SkipButton.onClick.AddListener(OnSkipClicked);
        }

        private void OnDisable()
        {
            if (m_Completion != null)
                Finish(ETutorialOutcome.Unavailable);
        }

        public UniTask<TutorialPresentResult> Play(TutorialDefinitionSO tutorial, Action<int> onPageShown)
        {
            if (m_Completion != null)
                Finish(ETutorialOutcome.Unavailable);

            m_Completion = new UniTaskCompletionSource<TutorialPresentResult>();
            m_Pages = tutorial.Pages;
            m_OnPageShown = onPageShown;
            m_PagesViewed = 0;
            m_CurrentDoneText = string.IsNullOrEmpty(tutorial.ContinueLabel) ? m_DoneText : tutorial.ContinueLabel;

            m_SkipButton.gameObject.SetActive(m_Pages.Count > 1);

            ShowPage(0);
            return m_Completion.Task;
        }

        private void ShowPage(int pageIndex)
        {
            m_PageIndex = pageIndex;
            m_PagesViewed = Mathf.Max(m_PagesViewed, pageIndex + 1);
            TutorialPage page = m_Pages[pageIndex];

            m_Title.gameObject.SetActive(!string.IsNullOrEmpty(page.Title));
            m_Title.text = page.Title;
            m_Body.text = page.Body;
            m_Image.gameObject.SetActive(page.Image != null);
            m_Image.sprite = page.Image;
            m_PageCounter.gameObject.SetActive(m_Pages.Count > 1);
            m_PageCounter.text = (pageIndex + 1) + "/" + m_Pages.Count;
            m_NextLabel.text = IsLastPage ? m_CurrentDoneText : m_NextText;

            m_NextButton.interactable = true;
            m_OnPageShown?.Invoke(pageIndex);
        }

        private bool IsLastPage => m_PageIndex >= m_Pages.Count - 1;

        private void OnNextClicked()
        {
            if (m_Completion == null)
                return;

            if (IsLastPage)
            {
                Finish(ETutorialOutcome.Completed);
                return;
            }

            ShowPage(m_PageIndex + 1);
        }

        private void OnSkipClicked()
        {
            if (m_Completion == null)
                return;

            Finish(ETutorialOutcome.Skipped);
        }

        private void Finish(ETutorialOutcome outcome)
        {
            m_NextButton.interactable = false;
            UniTaskCompletionSource<TutorialPresentResult> completion = m_Completion;
            m_Completion = null;
            m_OnPageShown = null;
            completion.TrySetResult(new TutorialPresentResult(outcome, m_PagesViewed));
        }
    }
}
