using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PillFrenzy.Gameplay
{
    [CreateAssetMenu(fileName = "TargetCatalog", menuName = "PillFrenzy/Target Catalog")]
    public sealed class TargetCatalogSO : ScriptableObject
    {
        [Serializable]
        private struct TargetCatalogEntry
        {
            [SerializeField] private ETargetCapacity m_Capacity;
            [SerializeField] private AssetReferenceGameObject m_Prefab;

            public ETargetCapacity Capacity => m_Capacity;
            public AssetReferenceGameObject Prefab => m_Prefab;
        }

        [SerializeField] private TargetCatalogEntry[] m_Entries;
        [SerializeField] private float m_Spacing = 2f;

        public float Spacing => m_Spacing;

        public bool TryGetPrefab(ETargetCapacity capacity, out AssetReferenceGameObject prefab)
        {
            for (int index = 0; index < m_Entries.Length; index++)
            {
                if (m_Entries[index].Capacity != capacity)
                    continue;

                prefab = m_Entries[index].Prefab;
                return prefab != null && prefab.RuntimeKeyIsValid();
            }

            prefab = null;
            return false;
        }
    }
}
