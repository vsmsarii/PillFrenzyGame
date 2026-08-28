namespace PillFrenzy.Core
{
    public readonly struct MatchStartAnalytics
    {
        public readonly int LevelIndex;

        public MatchStartAnalytics(int levelIndex)
        {
            LevelIndex = levelIndex;
        }
    }

    public readonly struct MatchWinAnalytics
    {
        public readonly int LevelIndex;
        public readonly float Time;

        public MatchWinAnalytics(int levelIndex, float time)
        {
            LevelIndex = levelIndex;
            Time = time;
        }
    }

    public readonly struct MatchLoseAnalytics
    {
        public readonly int LevelIndex;
        public readonly float ResultTime;
        public readonly int AttemptCount;

        public MatchLoseAnalytics(int levelIndex, float resultTime, int attemptCount)
        {
            LevelIndex = levelIndex;
            ResultTime = resultTime;
            AttemptCount = attemptCount;
        }
    }

    public readonly struct SpecialPowerUseAnalytics
    {
        public readonly int LevelIndex;
        public readonly ESpecialPowerId PowerId;
        public readonly float UsedSeconds;

        public SpecialPowerUseAnalytics(int levelIndex, ESpecialPowerId powerId, float usedSeconds)
        {
            LevelIndex = levelIndex;
            PowerId = powerId;
            UsedSeconds = usedSeconds;
        }
    }
}
