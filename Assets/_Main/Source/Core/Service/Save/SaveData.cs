using System;
using System.Collections.Generic;

namespace PillFrenzy.Core
{
    [Serializable]
    public sealed class SaveData
    {
        public int Version;
        public int CurrentLevelIndex;
        public bool FirstLevelCompleted;
        public int TotalScore;
        public int TotalAttempts;
        public int TotalCompletionSeconds;
        public List<LevelRecordData> LevelScores = new();
        public List<SpecialPowerSaveEntry> SpecialPowers = new();
        public List<string> SeenTutorials = new();
        public long ImmortalUntilUnixUtc;
        public int Hearts;
        public bool HeartsInitialized;
        public long NextHeartUnixUtc;
    }

    [Serializable]
    public sealed class LevelRecordData
    {
        public int LevelIndex;
        public int Score;
        public int Attempts;
        public int CompletionSeconds;
    }

    [Serializable]
    public sealed class SpecialPowerSaveEntry
    {
        public int PowerId;
        public int Charges;
        public bool InitialGranted;
    }
}
