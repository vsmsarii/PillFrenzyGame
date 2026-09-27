using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PillFrenzy.Gameplay
{
    public sealed class TargetFactory
    {
        private readonly IGameObjectPool m_Pool;
        private readonly TargetCatalogSO m_Catalog;
        private readonly HashSet<string> m_UsedPrefabKeys = new();

        public TargetFactory(IGameObjectPool pool, TargetCatalogSO catalog)
        {
            m_Pool = pool;
            m_Catalog = catalog;
        }

        public TargetCatalogSO Catalog => m_Catalog;

        public async UniTask<TargetController> Create(ETargetCapacity capacity, Transform parent, CancellationToken cancellationToken)
        {
            if (!m_Catalog.TryGetPrefab(capacity, out AssetReferenceGameObject prefab))
            {
                Logger.Error("TargetCatalog has no prefab for capacity " + capacity + ".");
                return null;
            }

            string prefabKey = prefab.RuntimeKey.ToString();
            m_UsedPrefabKeys.Add(prefabKey);
            return await m_Pool.Get<TargetController>(prefabKey, parent, cancellationToken);
        }

        public void Release(TargetController controller)
        {
            if (controller == null)
                return;

            controller.KillTweens();
            m_Pool.Release(controller.gameObject);
        }

        public void ReleasePooledInstances()
        {
            foreach (string prefabKey in m_UsedPrefabKeys)
                m_Pool.ReleaseInactive(prefabKey);

            m_UsedPrefabKeys.Clear();
        }
    }
}
