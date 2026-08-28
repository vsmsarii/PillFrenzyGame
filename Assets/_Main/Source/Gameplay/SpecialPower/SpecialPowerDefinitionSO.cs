using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [CreateAssetMenu(fileName = "SpecialPower", menuName = "PillFrenzy/Special Power Definition")]
    public sealed class SpecialPowerDefinitionSO : ScriptableObject
    {
        [SerializeField] private ESpecialPowerId m_Id = ESpecialPowerId.SlowConveyor;
        [SerializeField] private string m_DisplayName = "Slow";
        [SerializeField] private Sprite m_Icon;
        [SerializeField] private float m_Duration = 8f;
        [SerializeField] private float m_SpeedMultiplier = 0.5f;

        public ESpecialPowerId Id => m_Id;
        public string DisplayName => m_DisplayName;
        public Sprite Icon => m_Icon;
        public float Duration => m_Duration;
        public float SpeedMultiplier => m_SpeedMultiplier;
    }
}
