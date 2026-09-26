using UnityEngine;

namespace PillFrenzy.Core
{
    [CreateAssetMenu(fileName = "GlobalSettings", menuName = "PillFrenzy/Global Settings")]
    public sealed class GlobalSettingsSO : ScriptableObject
    {
        [Header("Hearts")]
        [SerializeField, Min(0)] private int m_DefaultHeartCount = 5;
        [SerializeField, Min(0f)] private float m_HeartRefillMinutes = 30f;

        [Header("Performance")]
        [SerializeField, Min(30)] private int m_TargetFrameRate = 60;

        [Header("Camera Framing")]
        [SerializeField, Range(0f, 0.5f)] private float m_HudTopInset = 0.1f;
        [SerializeField, Range(0f, 0.6f)] private float m_BoxRowTopInset = 0.3f;
        [SerializeField, Range(0f, 0.5f)] private float m_BoxRowViewportY = 0.14f;
        [SerializeField, Min(0f)] private float m_BoxRowSideMargin = 0.4f;
        [SerializeField, Min(1f)] private float m_BoxRowMinDepth = 8f;

        public int DefaultHeartCount => m_DefaultHeartCount;
        public float HeartRefillMinutes => m_HeartRefillMinutes;
        public int TargetFrameRate => m_TargetFrameRate;
        public float HudTopInset => m_HudTopInset;
        public float BoxRowTopInset => m_BoxRowTopInset;
        public float BoxRowViewportY => m_BoxRowViewportY;
        public float BoxRowSideMargin => m_BoxRowSideMargin;
        public float BoxRowMinDepth => m_BoxRowMinDepth;
    }
}
