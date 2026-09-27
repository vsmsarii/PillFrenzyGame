using System.Collections.Generic;

namespace PillFrenzy.Core
{
    public sealed class AnalyticsSystem : Service, IAnalyticsSystem
    {
        private readonly List<IAnalytics> m_Providers = new();

        public void Register(IAnalytics analytics)
        {
            m_Providers.Add(analytics);
        }

        protected override void OnInitialize()
        {
            EB.Analytics.Add<MatchStartAnalytics>(OnMatchStart);
            EB.Analytics.Add<MatchWinAnalytics>(OnMatchWin);
            EB.Analytics.Add<MatchLoseAnalytics>(OnMatchLose);
            EB.Analytics.Add<SpecialPowerUseAnalytics>(OnSpecialPowerUse);
            EB.Analytics.Add<TutorialStartAnalytics>(OnTutorialStart);
            EB.Analytics.Add<TutorialPageAnalytics>(OnTutorialPage);
            EB.Analytics.Add<TutorialEndAnalytics>(OnTutorialEnd);
            EB.Analytics.Add<AdShowAnalytics>(OnAdShow);
        }

        protected override void OnDispose()
        {
            EB.Analytics.Remove<MatchStartAnalytics>(OnMatchStart);
            EB.Analytics.Remove<MatchWinAnalytics>(OnMatchWin);
            EB.Analytics.Remove<MatchLoseAnalytics>(OnMatchLose);
            EB.Analytics.Remove<SpecialPowerUseAnalytics>(OnSpecialPowerUse);
            EB.Analytics.Remove<TutorialStartAnalytics>(OnTutorialStart);
            EB.Analytics.Remove<TutorialPageAnalytics>(OnTutorialPage);
            EB.Analytics.Remove<TutorialEndAnalytics>(OnTutorialEnd);
            EB.Analytics.Remove<AdShowAnalytics>(OnAdShow);
            m_Providers.Clear();
        }

        private void OnMatchStart(MatchStartAnalytics evt)
        {
            for (int i = 0; i < m_Providers.Count; i++)
                m_Providers[i].MatchStart(evt.LevelIndex);
        }

        private void OnMatchWin(MatchWinAnalytics evt)
        {
            for (int i = 0; i < m_Providers.Count; i++)
                m_Providers[i].MatchWin(evt.LevelIndex, evt.Time);
        }

        private void OnMatchLose(MatchLoseAnalytics evt)
        {
            for (int i = 0; i < m_Providers.Count; i++)
                m_Providers[i].MatchLose(evt.LevelIndex, evt.ResultTime, evt.AttemptCount);
        }

        private void OnSpecialPowerUse(SpecialPowerUseAnalytics evt)
        {
            for (int i = 0; i < m_Providers.Count; i++)
                m_Providers[i].SpecialPowerUse(evt.LevelIndex, evt.PowerId, evt.UsedSeconds);
        }

        private void OnTutorialStart(TutorialStartAnalytics evt)
        {
            for (int i = 0; i < m_Providers.Count; i++)
                m_Providers[i].TutorialStart(evt.TutorialId, evt.LevelIndex, evt.PageCount);
        }

        private void OnTutorialPage(TutorialPageAnalytics evt)
        {
            for (int i = 0; i < m_Providers.Count; i++)
                m_Providers[i].TutorialPage(evt.TutorialId, evt.LevelIndex, evt.PageIndex);
        }

        private void OnTutorialEnd(TutorialEndAnalytics evt)
        {
            for (int i = 0; i < m_Providers.Count; i++)
                m_Providers[i].TutorialEnd(evt.TutorialId, evt.LevelIndex, evt.PagesViewed, evt.Skipped, evt.Seconds);
        }

        private void OnAdShow(AdShowAnalytics evt)
        {
            for (int i = 0; i < m_Providers.Count; i++)
                m_Providers[i].AdShow(evt.LevelIndex, evt.Type, evt.Result);
        }
    }
}
