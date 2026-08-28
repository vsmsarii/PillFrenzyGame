namespace PillFrenzy.Core
{
    public interface IAnalytics
    {
        void MatchStart(int levelIndex);
        void MatchWin(int levelIndex, float time);
        void MatchLose(int levelIndex, float resultTime, int attemptCount);
        void SpecialPowerUse(int levelIndex, ESpecialPowerId powerId, float usedSeconds);
    }
}
