using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public readonly struct TutorialMomentView
    {
        public readonly TutorialPage Page;
        public readonly Transform Anchor;
        public readonly bool ShowPointer;
        public readonly bool ShowTapPrompt;

        public TutorialMomentView(TutorialPage page, Transform anchor, bool showPointer, bool showTapPrompt)
        {
            Page = page;
            Anchor = anchor;
            ShowPointer = showPointer;
            ShowTapPrompt = showTapPrompt;
        }
    }

    public interface ITutorialMomentPresenter
    {
        void Show(TutorialMomentView view);
        void Hide();
    }
}
