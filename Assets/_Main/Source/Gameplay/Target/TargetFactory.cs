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

        public TargetFactory(IGameObjectPool pool, TargetCatalogSO catalog)
        {
            m_Pool = pool;
            m_Catalog = catalog;
        }

        public float Spacing => m_Catalog != null ? m_Catalog.Spacing : 2f;

        public async UniTask<TargetController> Create(ETargetCapacity capacity, Transform parent, CancellationToken cancellationToken)
        {
            if (m_Catalog == null || !m_Catalog.TryGetPrefab(capacity, out AssetReferenceGameObject prefab))
            {
                Logger.Error("TargetCatalog has no prefab for capacity " + capacity + ".");
                return null;
            }

            return await m_Pool.Get<TargetController>(prefab.RuntimeKey.ToString(), parent, cancellationToken);
        }

        public void Release(TargetController controller)
        {
            if (controller == null)
                return;

            controller.KillExit();
            m_Pool.Release(controller.gameObject);
        }
    }
}
