using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct VfxEntry
    {
        [SerializeField] private EVfxId m_Id;
        [SerializeField] private AssetReferenceGameObject m_Prefab;
        [SerializeField] private Vector3 m_SpawnOffset;
        [SerializeField, Min(0)] private int m_Warmup;
        [SerializeField] private bool m_TintWithCapsuleColor;

        public EVfxId Id => m_Id;
        public AssetReferenceGameObject Prefab => m_Prefab;
        public Vector3 SpawnOffset => m_SpawnOffset;
        public int Warmup => m_Warmup;
        public bool TintWithCapsuleColor => m_TintWithCapsuleColor;
        public bool IsValid => m_Id != EVfxId.None && m_Prefab != null && m_Prefab.RuntimeKeyIsValid();
    }
}
