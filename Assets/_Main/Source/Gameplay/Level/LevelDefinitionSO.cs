using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct TargetQuota
    {
        [SerializeField] private CapsuleColorSO m_Color;
        [SerializeField] private ETargetCapacity m_Capacity;

        public CapsuleColorSO Color => m_Color;
        public ETargetCapacity Capacity => m_Capacity;
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
        [SerializeField] private CapsuleDefinitionSO m_NormalDefinition;
        [SerializeField, Range(0f, 1f)] private float m_GoldChance = 0.1f;
        [SerializeField] private CapsuleDefinitionSO m_GoldDefinition;
        [SerializeField, Range(0f, 1f)] private float m_PoisonChance = 0.1f;
        [SerializeField] private CapsuleDefinitionSO m_PoisonDefinition;

        [Header("Targets")]
        [SerializeField] private TargetQuota[] m_TargetQueue;

        [Header("Layout")]
        [SerializeField] private AssetReferenceGameObject m_Layout;

        [Header("Camera")]
        [SerializeField] private float m_CameraDistance;
        [SerializeField, Range(10f, 89f)] private float m_CameraPitch = 47.46f;
        [SerializeField] private float m_CameraYaw;
        [SerializeField, Range(20f, 90f)] private float m_FieldOfView = 60f;
        [SerializeField, Min(0f)] private float m_FramingPadding = 0.75f;

        public float SpawnInterval => m_SpawnInterval;
        public float MinSpawnInterval => Mathf.Min(m_SpawnInterval, m_MinSpawnInterval);
        public float ConveyorSpeed => m_ConveyorSpeed;
        public float MaxConveyorSpeed => Mathf.Max(m_ConveyorSpeed, m_MaxConveyorSpeed);
        public float SpeedRamp => m_SpeedRamp;
        public int MaxActive => m_MaxActive;
        public CapsuleDefinitionSO NormalDefinition => m_NormalDefinition;
        public TargetQuota[] TargetQueue => m_TargetQueue;
        public int StartingHealth => m_StartingHealth;
        public int ScorePerCorrect => m_ScorePerCorrect;
        public CapsuleDefinitionSO GoldDefinition => m_GoldDefinition;
        public CapsuleDefinitionSO PoisonDefinition => m_PoisonDefinition;
        public float GoldChance => m_GoldChance;
        public float PoisonChance => m_PoisonChance;
        public bool ReturnToMenu => m_ReturnToMenu;
        public AssetReferenceGameObject Layout => m_Layout;
        public float CameraDistance => m_CameraDistance;
        public Quaternion CameraRotation => Quaternion.Euler(m_CameraPitch, m_CameraYaw, 0f);
        public float FieldOfView => m_FieldOfView;
        public float FramingPadding => m_FramingPadding;

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
