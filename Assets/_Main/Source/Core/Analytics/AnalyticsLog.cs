using UnityEngine;

namespace PillFrenzy.Core
{
    public sealed class AnalyticsLog : IAnalytics
    {
        public void MatchStart(int levelIndex)
        {
            Debug.Log("[Analytics] MatchStart level_index=" + levelIndex);
        }

        public void MatchWin(int levelIndex, float time)
        {
            Debug.Log("[Analytics] MatchWin level_index=" + levelIndex + " time=" + time.ToString("0.###"));
        }

        public void MatchLose(int levelIndex, float resultTime, int attemptCount)
        {
            Debug.Log( "[Analytics] MatchLose level_index=" + levelIndex + " result_time=" + resultTime.ToString("0.###") + " attempt_count=" + attemptCount);
        }

        public void SpecialPowerUse(int levelIndex, ESpecialPowerId powerId, float usedSeconds)
        {
            Debug.Log("[Analytics] SpecialPowerUse level_index=" + levelIndex + " power_id=" + powerId + " used_seconds=" + usedSeconds.ToString("0.###"));
        }
    }
}
