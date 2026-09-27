using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class TutorialPreRunStep : IPreRunStep
    {
        private readonly TutorialSelector m_Selector;
        private readonly ITutorialPresenter m_Presenter;
        private readonly ISaveService m_Save;
        private readonly List<TutorialDefinitionSO> m_Pending = new();

        public TutorialPreRunStep(TutorialSelector selector, ITutorialPresenter presenter, ISaveService save)
        {
            m_Selector = selector;
            m_Presenter = presenter;
            m_Save = save;
        }

        public bool ShouldRun(PreRunContext context)
        {
            m_Selector.CollectPending(context.LevelIndex, context.Definition, true, m_Pending);
            return m_Pending.Count > 0;
        }

        public async UniTask RunAsync(PreRunContext context, CancellationToken cancellationToken)
        {
            for (int i = 0; i < m_Pending.Count; i++)
            {
                TutorialDefinitionSO tutorial = m_Pending[i];
                string id = tutorial.Id;
                int levelIndex = context.LevelIndex;
                float startedAt = Time.realtimeSinceStartup;

                EB.Analytics.Invoke(new TutorialStartAnalytics(id, levelIndex, tutorial.Pages.Count));
                TutorialPresentResult result = await m_Presenter.PresentAsync(
                    tutorial,
                    pageIndex => EB.Analytics.Invoke(new TutorialPageAnalytics(id, levelIndex, pageIndex)),
                    cancellationToken);

                if (result.Outcome == ETutorialOutcome.Unavailable)
                {
                    Logger.Warning("Tutorial panel unavailable. Skipping remaining tutorials.");
                    for (int j = i; j < m_Pending.Count; j++)
                        EB.Gameplay.Invoke(new TutorialFinished(m_Pending[j]));
                    break;
                }

                m_Save.MarkTutorialSeen(id);
                EB.Gameplay.Invoke(new TutorialFinished(tutorial));
                EB.Analytics.Invoke(new TutorialEndAnalytics(
                    id,
                    levelIndex,
                    result.PagesViewed,
                    result.Outcome == ETutorialOutcome.Skipped,
                    Time.realtimeSinceStartup - startedAt));
            }

            m_Pending.Clear();
        }
    }
}
