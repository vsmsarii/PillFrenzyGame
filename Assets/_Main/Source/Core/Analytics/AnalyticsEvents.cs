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

    public readonly struct TutorialStartAnalytics
    {
        public readonly string TutorialId;
        public readonly int LevelIndex;
        public readonly int PageCount;

        public TutorialStartAnalytics(string tutorialId, int levelIndex, int pageCount)
        {
            TutorialId = tutorialId;
            LevelIndex = levelIndex;
            PageCount = pageCount;
        }
    }

    public readonly struct TutorialPageAnalytics
    {
        public readonly string TutorialId;
        public readonly int LevelIndex;
        public readonly int PageIndex;

        public TutorialPageAnalytics(string tutorialId, int levelIndex, int pageIndex)
        {
            TutorialId = tutorialId;
            LevelIndex = levelIndex;
            PageIndex = pageIndex;
        }
    }

    public readonly struct TutorialEndAnalytics
    {
        public readonly string TutorialId;
        public readonly int LevelIndex;
        public readonly int PagesViewed;
        public readonly bool Skipped;
        public readonly float Seconds;

        public TutorialEndAnalytics(string tutorialId, int levelIndex, int pagesViewed, bool skipped, float seconds)
        {
            TutorialId = tutorialId;
            LevelIndex = levelIndex;
            PagesViewed = pagesViewed;
            Skipped = skipped;
            Seconds = seconds;
        }
    }

    public readonly struct AdShowAnalytics
    {
        public readonly int LevelIndex;
        public readonly EAdType Type;
        public readonly EAdResult Result;

        public AdShowAnalytics(int levelIndex, EAdType type, EAdResult result)
        {
            LevelIndex = levelIndex;
            Type = type;
            Result = result;
        }
    }
}
