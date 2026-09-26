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

        public TargetCatalogSO Catalog => m_Catalog;

        public async UniTask<TargetController> Create(ETargetCapacity capacity, Transform parent, CancellationToken cancellationToken)
        {
            if (!m_Catalog.TryGetPrefab(capacity, out AssetReferenceGameObject prefab))
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

            controller.KillTweens();
            m_Pool.Release(controller.gameObject);
        }
    }
}
