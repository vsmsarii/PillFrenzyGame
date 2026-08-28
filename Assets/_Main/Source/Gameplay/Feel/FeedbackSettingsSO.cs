using System;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct FeedbackProfile
    {
        [SerializeField] private float m_ShakeDuration;
        [SerializeField] private float m_ShakeStrength;
        [SerializeField] private bool m_HasBurst;
        [SerializeField] private float m_BurstStartScale;
        [SerializeField] private float m_BurstEndScale;
        [SerializeField] private float m_BurstGrowDuration;
        [SerializeField] private float m_BurstLifetimeSeconds;

        public float ShakeDuration => m_ShakeDuration;
        public float ShakeStrength => m_ShakeStrength;
        public bool HasBurst => m_HasBurst;
        public float BurstStartScale => m_BurstStartScale <= 0f ? 0.18f : m_BurstStartScale;
        public float BurstEndScale => m_BurstEndScale <= 0f ? 0.65f : m_BurstEndScale;
        public float BurstGrowDuration => m_BurstGrowDuration <= 0f ? 0.28f : m_BurstGrowDuration;
        public float BurstLifetimeSeconds => m_BurstLifetimeSeconds <= 0f ? 0.32f : m_BurstLifetimeSeconds;

        public static FeedbackProfile Create(float shakeDuration, float shakeStrength, bool hasBurst)
        {
            return new FeedbackProfile
            {
                m_ShakeDuration = shakeDuration,
                m_ShakeStrength = shakeStrength,
                m_HasBurst = hasBurst,
                m_BurstStartScale = 0.18f,
                m_BurstEndScale = 0.65f,
                m_BurstGrowDuration = 0.28f,
                m_BurstLifetimeSeconds = 0.32f
            };
        }
    }

    [CreateAssetMenu(fileName = "FeedbackSettings", menuName = "PillFrenzy/Feedback Settings")]
    public sealed class FeedbackSettingsSO : ScriptableObject
    {
        [Header("Shake")]
        [SerializeField, Min(1)] private int m_ShakeVibrato = 14;
        [SerializeField, Range(0f, 180f)] private float m_ShakeRandomness = 90f;

        [Header("Profiles")]
        [SerializeField] private FeedbackProfile m_Correct = FeedbackProfile.Create(0.16f, 0.08f, true);
        [SerializeField] private FeedbackProfile m_Gold = FeedbackProfile.Create(0.18f, 0.1f, true);
        [SerializeField] private FeedbackProfile m_Poison = FeedbackProfile.Create(0.28f, 0.22f, true);
        [SerializeField] private FeedbackProfile m_Complete = FeedbackProfile.Create(0.32f, 0.14f, false);
        [SerializeField] private FeedbackProfile m_Fail = FeedbackProfile.Create(0.36f, 0.18f, false);

        public int ShakeVibrato => m_ShakeVibrato;
        public float ShakeRandomness => m_ShakeRandomness;
        public FeedbackProfile Correct => m_Correct;
        public FeedbackProfile Gold => m_Gold;
        public FeedbackProfile Poison => m_Poison;
        public FeedbackProfile Complete => m_Complete;
        public FeedbackProfile Fail => m_Fail;
    }
}
