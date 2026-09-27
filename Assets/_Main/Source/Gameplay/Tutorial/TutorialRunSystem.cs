using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class TutorialRunSystem : ITickable, ICapsuleTapInterceptor
    {
        private const float MinPageSeconds = 0.35f;
        private const float ViewportMargin = 0.05f;

        private readonly TutorialSelector m_Selector;
        private readonly ITutorialMomentPresenter m_Presenter;
        private readonly ITutorialPresenter m_ModalPresenter;
        private readonly CancellationToken m_Token;
        private readonly ISaveService m_Save;
        private readonly LevelSystem m_Level;
        private readonly CapsuleSystem m_Capsules;
        private readonly Camera m_Camera;
        private readonly List<TutorialDefinitionSO> m_Pending = new();

        private TutorialDefinitionSO m_Active;
        private CapsuleController m_Anchor;
        private int m_PageIndex;
        private int m_PagesViewed;
        private float m_PageShownFor;
        private float m_StartedAt;
        private int m_SpawnedCount;
        private int m_ResolvedCount;

        private bool IsLastPage => m_PageIndex >= m_Active.Pages.Count - 1;

        public TutorialRunSystem(
            TutorialSelector selector,
            ITutorialMomentPresenter presenter,
            ITutorialPresenter modalPresenter,
            ISaveService save,
            LevelSystem level,
            CapsuleSystem capsules,
            Camera camera,
            CancellationToken token)
        {
            m_Selector = selector;
            m_Presenter = presenter;
            m_ModalPresenter = modalPresenter;
            m_Token = token;
            m_Save = save;
            m_Level = level;
            m_Capsules = capsules;
            m_Camera = camera;

            EB.Gameplay.Add<RunStarted>(OnRunStarted);
            EB.Gameplay.Add<RunFinished>(OnRunFinished);
            EB.Gameplay.Add<CapsuleSpawned>(OnCapsuleSpawned);
            EB.Gameplay.Add<CapsuleResolved>(OnCapsuleResolved);
        }

        public void Tick(float deltaTime)
        {
            if (m_Active != null)
            {
                m_PageShownFor += deltaTime;
                return;
            }

            if (!m_Level.IsSimulating)
                return;

            for (int i = 0; i < m_Pending.Count; i++)
            {
                if (IsReadyToShow(m_Pending[i], out CapsuleController anchor))
                {
                    Begin(i, anchor);
                    return;
                }
            }
        }

        public bool TryIntercept(CapsuleController tapped)
        {
            if (m_Active == null)
                return false;

            if (m_Active.Presentation == ETutorialPresentation.Modal || m_PageShownFor < MinPageSeconds)
                return true;

            if (!IsLastPage)
            {
                ShowPage(m_PageIndex + 1);
                return true;
            }

            bool needsAnchorTap = m_Active.Completion == ETutorialCompletion.TapHighlighted;
            if (needsAnchorTap && tapped != m_Anchor)
                return true;

            Complete(m_PagesViewed, false);
            return !needsAnchorTap;
        }

        public void Shutdown()
        {
            EB.Gameplay.Remove<RunStarted>(OnRunStarted);
            EB.Gameplay.Remove<RunFinished>(OnRunFinished);
            EB.Gameplay.Remove<CapsuleSpawned>(OnCapsuleSpawned);
            EB.Gameplay.Remove<CapsuleResolved>(OnCapsuleResolved);

            if (m_Active != null && m_Active.Presentation == ETutorialPresentation.Overlay)
                m_Presenter.Hide();
        }

        private void OnRunStarted(RunStarted evt)
        {
            m_SpawnedCount = 0;
            m_ResolvedCount = 0;
            m_Selector.CollectPending(m_Level.LevelIndex, evt.Definition, false, m_Pending);
        }

        private void OnRunFinished(RunFinished evt)
        {
            if (m_Active != null)
                Close();

            m_Pending.Clear();
        }

        private void OnCapsuleSpawned(CapsuleSpawned evt)
        {
            m_SpawnedCount++;
        }

        private void OnCapsuleResolved(CapsuleResolved evt)
        {
            m_ResolvedCount++;
        }

        private bool IsReadyToShow(TutorialDefinitionSO tutorial, out CapsuleController anchor)
        {
            anchor = null;
            switch (tutorial.Moment)
            {
                case ETutorialMoment.CapsuleEntered:
                    return TryFindEnteredCapsule(tutorial, out anchor);
                case ETutorialMoment.AfterCapsulesSpawned:
                    return m_SpawnedCount >= tutorial.CapsuleCount;
                case ETutorialMoment.AfterCapsulesResolved:
                    return m_ResolvedCount >= tutorial.CapsuleCount;
                default:
                    return false;
            }
        }

        private bool TryFindEnteredCapsule(TutorialDefinitionSO tutorial, out CapsuleController anchor)
        {
            IReadOnlyList<CapsuleController> capsules = m_Capsules.Active;
            for (int i = 0; i < capsules.Count; i++)
            {
                CapsuleController capsule = capsules[i];
                bool entered = capsule.State == ECapsuleState.OnPath
                    && capsule.Definition.Kind == tutorial.CapsuleKind
                    && capsule.Distance >= tutorial.EnterDistance
                    && IsOnScreen(capsule.transform.position);

                if (entered)
                {
                    anchor = capsule;
                    return true;
                }
            }

            anchor = null;
            return false;
        }

        private bool IsOnScreen(Vector3 worldPosition)
        {
            Vector3 viewport = m_Camera.WorldToViewportPoint(worldPosition);
            return viewport.z > 0f
                && viewport.x >= ViewportMargin && viewport.x <= 1f - ViewportMargin
                && viewport.y >= ViewportMargin && viewport.y <= 1f - ViewportMargin;
        }

        private void Begin(int pendingIndex, CapsuleController anchor)
        {
            m_Active = m_Pending[pendingIndex];
            m_Pending.RemoveAt(pendingIndex);
            m_Anchor = anchor;
            m_PagesViewed = 0;
            m_StartedAt = Time.realtimeSinceStartup;

            m_Level.Pause(EPauseReason.Tutorial);
            EB.Analytics.Invoke(new TutorialStartAnalytics(m_Active.Id, m_Level.LevelIndex, m_Active.Pages.Count));

            if (m_Active.Presentation == ETutorialPresentation.Modal)
                PresentModalAsync(m_Active).Forget();
            else
                ShowPage(0);
        }

        private async UniTaskVoid PresentModalAsync(TutorialDefinitionSO tutorial)
        {
            string id = tutorial.Id;
            int levelIndex = m_Level.LevelIndex;
            (bool canceled, TutorialPresentResult result) = await m_ModalPresenter.PresentAsync(
                    tutorial,
                    pageIndex => EB.Analytics.Invoke(new TutorialPageAnalytics(id, levelIndex, pageIndex)),
                    m_Token)
                .SuppressCancellationThrow();

            if (canceled || m_Active != tutorial)
                return;

            if (result.Outcome == ETutorialOutcome.Unavailable)
            {
                Logger.Warning("Tutorial panel unavailable: " + id);
                Close();
                EB.Gameplay.Invoke(new TutorialFinished(tutorial));
                return;
            }

            Complete(result.PagesViewed, result.Outcome == ETutorialOutcome.Skipped);
        }

        private void ShowPage(int pageIndex)
        {
            m_PageIndex = pageIndex;
            m_PageShownFor = 0f;
            m_PagesViewed = Mathf.Max(m_PagesViewed, pageIndex + 1);

            bool waitsForAnchorTap = IsLastPage && m_Active.Completion == ETutorialCompletion.TapHighlighted;
            m_Presenter.Show(new TutorialMomentView(
                m_Active.Pages[pageIndex],
                m_Anchor != null ? m_Anchor.transform : null,
                m_Active.ShowPointer,
                !waitsForAnchorTap));

            EB.Analytics.Invoke(new TutorialPageAnalytics(m_Active.Id, m_Level.LevelIndex, pageIndex));
        }

        private void Complete(int pagesViewed, bool skipped)
        {
            TutorialDefinitionSO tutorial = Close();
            m_Save.MarkTutorialSeen(tutorial.Id);
            EB.Analytics.Invoke(new TutorialEndAnalytics(
                tutorial.Id,
                m_Level.LevelIndex,
                pagesViewed,
                skipped,
                Time.realtimeSinceStartup - m_StartedAt));
            EB.Gameplay.Invoke(new TutorialFinished(tutorial));
        }

        private TutorialDefinitionSO Close()
        {
            TutorialDefinitionSO tutorial = m_Active;
            m_Active = null;
            m_Anchor = null;

            if (tutorial.Presentation == ETutorialPresentation.Overlay)
                m_Presenter.Hide();

            m_Level.Resume(EPauseReason.Tutorial);
            return tutorial;
        }
    }
}
