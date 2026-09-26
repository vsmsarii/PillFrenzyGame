using System;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct FeedbackProfile
    {
        [SerializeField] private float m_ShakeDuration;
        [SerializeField] private float m_ShakeStrength;

        public float ShakeDuration => m_ShakeDuration;
        public float ShakeStrength => m_ShakeStrength;

        public static FeedbackProfile Create(float shakeDuration, float shakeStrength)
        {
            return new FeedbackProfile
            {
                m_ShakeDuration = shakeDuration,
                m_ShakeStrength = shakeStrength
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
        [SerializeField] private FeedbackProfile m_Correct = FeedbackProfile.Create(0.16f, 0.08f);
        [SerializeField] private FeedbackProfile m_Gold = FeedbackProfile.Create(0.18f, 0.1f);
        [SerializeField] private FeedbackProfile m_Poison = FeedbackProfile.Create(0.28f, 0.22f);
        [SerializeField] private FeedbackProfile m_Complete = FeedbackProfile.Create(0.32f, 0.14f);
        [SerializeField] private FeedbackProfile m_Fail = FeedbackProfile.Create(0.36f, 0.18f);

        public int ShakeVibrato => m_ShakeVibrato;
        public float ShakeRandomness => m_ShakeRandomness;
        public FeedbackProfile Correct => m_Correct;
        public FeedbackProfile Gold => m_Gold;
        public FeedbackProfile Poison => m_Poison;
        public FeedbackProfile Complete => m_Complete;
        public FeedbackProfile Fail => m_Fail;
    }
}
