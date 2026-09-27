namespace PillFrenzy.Core
{
    public interface IAnalytics
    {
        void MatchStart(int levelIndex);
        void MatchWin(int levelIndex, float time);
        void MatchLose(int levelIndex, float resultTime, int attemptCount);
        void SpecialPowerUse(int levelIndex, ESpecialPowerId powerId, float usedSeconds);
        void TutorialStart(string tutorialId, int levelIndex, int pageCount);
        void TutorialPage(string tutorialId, int levelIndex, int pageIndex);
        void TutorialEnd(string tutorialId, int levelIndex, int pagesViewed, bool skipped, float seconds);
        void AdShow(int levelIndex, EAdType type, EAdResult result);
    }
}
