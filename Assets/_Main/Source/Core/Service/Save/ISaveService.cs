namespace PillFrenzy.Core
{
    public interface ISaveService : IService
    {
        int CurrentLevelIndex { get; }
        int CurrentLevelNumber { get; }
        bool HasCompletedFirstLevel { get; }
        int Hearts { get; }
        int MaxHearts { get; }
        long SecondsUntilNextHeart { get; }
        long ImmortalRemainingSeconds { get; }

        int GetLevelScore(int levelIndex);
        int GetTotalScore();
        int GetTotalAttempts();
        int GetTotalCompletionSeconds();
        int GetLevelAttempts(int levelIndex);
        void CompleteLevel(int levelIndex, int score, int completionSeconds);
        int IncrementLevelAttempts(int levelIndex);

        int GetSpecialPowerCharges(ESpecialPowerId id);
        bool TryConsumeSpecialPowerCharge(ESpecialPowerId id);
        void AddSpecialPowerCharges(ESpecialPowerId id, int amount);
        bool TryGrantInitialSpecialPower(ESpecialPowerId id, int charges);

        bool IsImmortalActive { get; }
        void GrantImmortalityMinutes(int minutes);

        void ConfigureHearts(int maxHeartCount, float refillMinutes);
        bool TrySpendHeart();
        void GrantHearts(int amount);
        void RefreshHearts();
        void FlushPending();
    }
}
