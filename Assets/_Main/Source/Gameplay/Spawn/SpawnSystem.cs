using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace PillFrenzy.Gameplay
{
    public sealed class SpawnSystem
    {
        private readonly CapsuleFactory m_Factory;
        private readonly CapsuleSystem m_Capsules;
        private readonly List<CapsuleController> m_DespawnBuffer = new();

        public SpawnSystem(CapsuleFactory factory, CapsuleSystem capsules)
        {
            m_Factory = factory;
            m_Capsules = capsules;
        }

        public async UniTask Spawn(CapsuleSpawnData data, IConveyorPath path, CancellationToken cancellationToken)
        {
            CapsuleController controller = await m_Factory.Create(data, path, cancellationToken);
            if (controller == null)
                return;

            m_Capsules.Register(controller);
        }

        public void Detach(CapsuleController controller)
        {
            if (controller == null)
                return;

            m_Capsules.Unregister(controller);
        }

        public void Despawn(CapsuleController controller)
        {
            if (controller == null)
                return;

            m_Capsules.Unregister(controller);
            m_Factory.Release(controller);
        }

        public void DespawnSeated(TargetSystem targets)
        {
            if (targets == null)
                return;

            targets.CollectSeated(m_DespawnBuffer);
            for (int i = 0; i < m_DespawnBuffer.Count; i++)
                Despawn(m_DespawnBuffer[i]);

            m_DespawnBuffer.Clear();
        }

        public void DespawnAll()
        {
            m_Capsules.CopyActive(m_DespawnBuffer);
            for (int i = 0; i < m_DespawnBuffer.Count; i++)
                Despawn(m_DespawnBuffer[i]);

            m_DespawnBuffer.Clear();
        }
    }
}
