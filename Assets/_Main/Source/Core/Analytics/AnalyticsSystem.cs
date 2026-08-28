using System.Collections.Generic;

namespace PillFrenzy.Core
{
    public sealed class AnalyticsSystem : Service, IAnalyticsSystem
    {
        private readonly List<IAnalytics> m_Providers = new List<IAnalytics>();

        public void Register(IAnalytics analytics)
        {
            if (analytics == null || m_Providers.Contains(analytics))
                return;

            m_Providers.Add(analytics);
        }

        protected override void OnInitialize()
        {
            EB.Analytics.Add<MatchStartAnalytics>(OnMatchStart);
            EB.Analytics.Add<MatchWinAnalytics>(OnMatchWin);
            EB.Analytics.Add<MatchLoseAnalytics>(OnMatchLose);
            EB.Analytics.Add<SpecialPowerUseAnalytics>(OnSpecialPowerUse);
        }

        protected override void OnDispose()
        {
            EB.Analytics.Remove<MatchStartAnalytics>(OnMatchStart);
            EB.Analytics.Remove<MatchWinAnalytics>(OnMatchWin);
            EB.Analytics.Remove<MatchLoseAnalytics>(OnMatchLose);
            EB.Analytics.Remove<SpecialPowerUseAnalytics>(OnSpecialPowerUse);
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
    }
}
