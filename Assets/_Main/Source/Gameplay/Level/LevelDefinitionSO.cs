using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct TargetQuota
    {
        [SerializeField] private ECapsuleColor m_Color;
        [SerializeField] private ETargetCapacity m_Amount;

        public ECapsuleColor Color => m_Color;
        public ETargetCapacity Capacity => m_Amount;
        public int Amount => (int)m_Amount;
    }

    [CreateAssetMenu(menuName = "PillFrenzy/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinitionSO : ScriptableObject
    {
        [Header("Run")]
        [SerializeField, Min(1)] private int m_StartingHealth = 3;
        [SerializeField, Min(1)] private int m_ScorePerCorrect = 10;
        [SerializeField] private bool m_ReturnToMenu;

        [Header("Spawn")]
        [SerializeField, Min(0.05f)] private float m_SpawnInterval = 1.25f;
        [SerializeField, Min(0.05f)] private float m_MinSpawnInterval = 0.5f;
        [SerializeField, Min(1)] private int m_MaxActive = 6;

        [Header("Conveyor")]
        [SerializeField, Min(0f)] private float m_ConveyorSpeed = 2.5f;
        [SerializeField, Min(0f)] private float m_MaxConveyorSpeed = 5f;
        [SerializeField, Min(0f)] private float m_SpeedRamp = 0.12f;

        [Header("Capsules")]
        [SerializeField] private CapsuleDefinitionSO[] m_CapsuleDefinitions;
        [SerializeField, Range(0f, 1f)] private float m_GoldChance = 0.1f;
        [SerializeField] private CapsuleDefinitionSO m_GoldDefinition;
        [SerializeField, Range(0f, 1f)] private float m_PoisonChance = 0.1f;
        [SerializeField] private CapsuleDefinitionSO m_PoisonDefinition;

        [Header("Targets")]
        [SerializeField] private TargetQuota[] m_TargetQuotas;

        [Header("Layout")]
        [SerializeField] private AssetReferenceGameObject m_Layout;

        public float SpawnInterval => m_SpawnInterval;
        public float MinSpawnInterval => Mathf.Min(m_SpawnInterval, m_MinSpawnInterval);
        public float ConveyorSpeed => m_ConveyorSpeed;
        public float MaxConveyorSpeed => Mathf.Max(m_ConveyorSpeed, m_MaxConveyorSpeed);
        public float SpeedRamp => m_SpeedRamp;
        public int MaxActive => m_MaxActive;
        public CapsuleDefinitionSO[] CapsuleDefinitions => m_CapsuleDefinitions;
        public TargetQuota[] TargetQuotas => m_TargetQuotas;
        public int StartingHealth => m_StartingHealth;
        public int ScorePerCorrect => m_ScorePerCorrect;
        public CapsuleDefinitionSO GoldDefinition => m_GoldDefinition;
        public CapsuleDefinitionSO PoisonDefinition => m_PoisonDefinition;
        public float GoldChance => m_GoldChance;
        public float PoisonChance => m_PoisonChance;
        public bool ReturnToMenu => m_ReturnToMenu;
        public AssetReferenceGameObject Layout => m_Layout;

        public bool TryGetLayoutKey(out string key)
        {
            key = null;
            if (m_Layout == null || !m_Layout.RuntimeKeyIsValid())
                return false;

            key = m_Layout.RuntimeKey.ToString();
            return true;
        }

        private void OnValidate()
        {
            if (m_MinSpawnInterval > m_SpawnInterval)
                m_MinSpawnInterval = m_SpawnInterval;

            if (m_MaxConveyorSpeed < m_ConveyorSpeed)
                m_MaxConveyorSpeed = m_ConveyorSpeed;

            if (m_GoldChance + m_PoisonChance > 1f)
                m_GoldChance = 1f - m_PoisonChance;
        }
    }
}
