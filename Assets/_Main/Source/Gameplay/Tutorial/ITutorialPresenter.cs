using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PillFrenzy.Gameplay
{
    public enum ETutorialOutcome
    {
        Completed = 0,
        Skipped,
        Unavailable
    }

    public readonly struct TutorialPresentResult
    {
        public readonly ETutorialOutcome Outcome;
        public readonly int PagesViewed;

        public TutorialPresentResult(ETutorialOutcome outcome, int pagesViewed)
        {
            Outcome = outcome;
            PagesViewed = pagesViewed;
        }
    }

    public interface ITutorialPresenter
    {
        UniTask<TutorialPresentResult> PresentAsync(
            TutorialDefinitionSO tutorial,
            Action<int> onPageShown,
            CancellationToken cancellationToken);
    }
}
