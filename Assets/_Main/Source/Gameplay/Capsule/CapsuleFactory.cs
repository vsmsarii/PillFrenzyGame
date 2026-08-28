using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class CapsuleFactory
    {
        private readonly IGameObjectPool m_Pool;
        private readonly Transform m_Parent;

        public CapsuleFactory(IGameObjectPool pool, Transform parent)
        {
            m_Pool = pool;
            m_Parent = parent;
        }

        public async UniTask<CapsuleController> Create(CapsuleSpawnData data, IConveyorPath path, CancellationToken cancellationToken)
        {
            CapsuleController controller = await m_Pool.Get<CapsuleController>(data.Definition.PrefabKey, m_Parent, cancellationToken);
            if (controller == null)
                return null;

            controller.Initialize(data, path);
            return controller;
        }

        public void Release(CapsuleController controller)
        {
            if (controller == null)
                return;

            controller.KillFlight();
            m_Pool.Release(controller.gameObject);
        }
    }
}
