using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class TargetSystem
    {
        private readonly ColorCatalogSO m_ColorTable;
        private readonly TargetFactory m_Factory;
        private readonly List<TargetController> m_Targets = new();
        private TargetFill[] m_Fills;

        public TargetSystem(ColorCatalogSO colorTable, TargetFactory factory)
        {
            m_ColorTable = colorTable;
            m_Factory = factory;
        }

        public async UniTask Bind(LevelDefinitionSO definition, LevelLayout layout, CancellationToken cancellationToken)
        {
            Transform origin = layout != null ? layout.TargetSpawnPoint : null;
            if (origin == null)
            {
                Logger.Error("Level prefab is missing target spawn point.");
                return;
            }

            TargetQuota[] quotas = definition != null ? definition.TargetQuotas : null;
            if (quotas == null || quotas.Length == 0)
            {
                Logger.Error("LevelDefinition has no target quotas.");
                return;
            }

            HideExistingChildren(origin);

            float spacing = m_Factory.Spacing;
            for (int i = 0; i < quotas.Length; i++)
            {
                TargetQuota quota = quotas[i];
                TargetController target = await m_Factory.Create(quota.Capacity, origin, cancellationToken);
                if (target == null || cancellationToken.IsCancellationRequested)
                    return;

                target.transform.SetLocalPositionAndRotation(Vector3.left * (i * spacing), Quaternion.identity);
                Register(target, layout.TargetExit, quota.Color, quota.Amount);
            }

            PublishFill();
        }

        public bool TryGet(ECapsuleColor color, out TargetController target)
        {
            for (int i = 0; i < m_Targets.Count; i++)
            {
                TargetController candidate = m_Targets[i];
                if (candidate != null && candidate.CapsuleColor == color && candidate.CanAccept)
                {
                    target = candidate;
                    return true;
                }
            }

            target = null;
            return false;
        }

        public bool AreAllFull()
        {
            if (m_Targets.Count == 0)
                return false;

            for (int i = 0; i < m_Targets.Count; i++)
            {
                TargetController target = m_Targets[i];
                if (target == null || !target.IsFilled)
                    return false;
            }

            return true;
        }

        public void CollectSeated(List<CapsuleController> buffer)
        {
            for (int i = 0; i < m_Targets.Count; i++)
            {
                TargetController target = m_Targets[i];
                if (target == null)
                    continue;

                target.KillExit();
                target.CollectSeated(buffer);
            }
        }

        public void Shutdown()
        {
            for (int i = 0; i < m_Targets.Count; i++)
                m_Factory.Release(m_Targets[i]);

            m_Targets.Clear();
            m_Fills = null;
        }

        private void Register(TargetController target, Transform fallbackExit, ECapsuleColor color, int capacity)
        {
            if (target == null)
                return;

            target.Initialize(color, capacity, fallbackExit, m_ColorTable);
            m_Targets.Add(target);
        }

        public void PublishFill()
        {
            if (m_Fills == null || m_Fills.Length != m_Targets.Count)
                m_Fills = new TargetFill[m_Targets.Count];

            for (int i = 0; i < m_Targets.Count; i++)
            {
                TargetController target = m_Targets[i];
                if (target == null)
                {
                    m_Fills[i] = default;
                    continue;
                }

                m_Fills[i] = new TargetFill(target.CapsuleColor, target.Occupied, target.Capacity);
            }

            EB.Presentation.Invoke(new RunTargetFillChanged(m_Fills));
        }

        private static void HideExistingChildren(Transform origin)
        {
            int count = origin.childCount;
            if (count == 0)
                return;

            Transform[] children = new Transform[count];
            for (int i = 0; i < count; i++)
                children[i] = origin.GetChild(i);

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                    children[i].gameObject.SetActive(false);
            }
        }
    }
}
