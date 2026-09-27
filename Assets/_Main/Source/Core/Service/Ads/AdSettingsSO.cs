using UnityEngine;

namespace PillFrenzy.Core
{
    [CreateAssetMenu(fileName = "AdSettings", menuName = "PillFrenzy/Ad Settings")]
    public sealed class AdSettingsSO : ScriptableObject
    {
        [Header("Placement")]
        [SerializeField] private EAdType m_Type = EAdType.Interstitial;
        [SerializeField, Min(1)] private int m_LevelInterval = 3;

        [Header("Ad Units")]
        [SerializeField] private bool m_TestAds = true;
        [SerializeField] private string m_TestAdUnitId;
        [SerializeField] private string m_ProductionAdUnitId;

        public EAdType Type => m_Type;
        public int LevelInterval => m_LevelInterval;
        public bool TestAds => m_TestAds;
        public string AdUnitId => m_TestAds ? m_TestAdUnitId : m_ProductionAdUnitId;
    }
}
