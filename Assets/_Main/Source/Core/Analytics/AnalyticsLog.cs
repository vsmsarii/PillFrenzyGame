namespace PillFrenzy.Core
{
    public sealed class AnalyticsLog : IAnalytics
    {
        public void MatchStart(int levelIndex)
        {
            Logger.Log("[Analytics] MatchStart level_index=" + levelIndex);
        }

        public void MatchWin(int levelIndex, float time)
        {
            Logger.Log("[Analytics] MatchWin level_index=" + levelIndex + " time=" + time.ToString("0.###"));
        }

        public void MatchLose(int levelIndex, float resultTime, int attemptCount)
        {
            Logger.Log("[Analytics] MatchLose level_index=" + levelIndex + " result_time=" + resultTime.ToString("0.###") + " attempt_count=" + attemptCount);
        }

        public void SpecialPowerUse(int levelIndex, ESpecialPowerId powerId, float usedSeconds)
        {
            Logger.Log("[Analytics] SpecialPowerUse level_index=" + levelIndex + " power_id=" + powerId + " used_seconds=" + usedSeconds.ToString("0.###"));
        }

        public void TutorialStart(string tutorialId, int levelIndex, int pageCount)
        {
            Logger.Log("[Analytics] TutorialStart tutorial_id=" + tutorialId + " level_index=" + levelIndex + " page_count=" + pageCount);
        }

        public void TutorialPage(string tutorialId, int levelIndex, int pageIndex)
        {
            Logger.Log("[Analytics] TutorialPage tutorial_id=" + tutorialId + " level_index=" + levelIndex + " page_index=" + pageIndex);
        }

        public void TutorialEnd(string tutorialId, int levelIndex, int pagesViewed, bool skipped, float seconds)
        {
            Logger.Log("[Analytics] TutorialEnd tutorial_id=" + tutorialId + " level_index=" + levelIndex + " pages_viewed=" + pagesViewed + " skipped=" + skipped + " seconds=" + seconds.ToString("0.###"));
        }

        public void AdShow(int levelIndex, EAdType type, EAdResult result)
        {
            Logger.Log("[Analytics] AdShow level_index=" + levelIndex + " type=" + type + " result=" + result);
        }
    }
}
