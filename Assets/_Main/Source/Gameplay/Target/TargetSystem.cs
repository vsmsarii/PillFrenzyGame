using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class TargetSystem
    {
        private readonly TargetFactory m_Factory;
        private readonly List<TargetController> m_Visible = new();

        private TargetQuota[] m_Queue;
        private int m_NextIndex;
        private Transform m_Origin;
        private Transform m_FallbackExit;
        private CancellationToken m_Token;
        private bool m_Filling;
        private TargetFill[] m_Fills;

        private TargetCatalogSO Catalog => m_Factory.Catalog;

        public bool IsComplete => m_NextIndex >= m_Queue.Length && m_Visible.Count == 0 && !m_Filling;

        public TargetSystem(TargetFactory factory)
        {
            m_Factory = factory;
        }

        public async UniTask Bind(LevelDefinitionSO definition, LevelLayout layout, CancellationToken cancellationToken)
        {
            m_Origin = layout.TargetSpawnPoint;
            m_Queue = definition.TargetQueue;
            if (m_Queue.Length == 0)
            {
                Logger.Error("LevelDefinition has an empty target queue.");
                return;
            }

            m_FallbackExit = layout.TargetExit;
            m_Token = cancellationToken;
            HideExistingChildren(m_Origin);

            while (m_Visible.Count < Catalog.VisibleCount && m_NextIndex < m_Queue.Length)
            {
                TargetController target = await SpawnNext();
                if (target == null)
                    return;

                target.transform.localPosition = SlotPosition(m_Visible.Count - 1);
            }

            PublishFill();
        }

        public bool TryGet(CapsuleColorSO color, out TargetController target)
        {
            for (int i = 0; i < m_Visible.Count; i++)
            {
                TargetController candidate = m_Visible[i];
                if (candidate.CapsuleColor == color && candidate.CanAccept)
                {
                    target = candidate;
                    return true;
                }
            }

            target = null;
            return false;
        }

        public void Advance(TargetController target)
        {
            if (!m_Visible.Remove(target))
                return;

            m_Factory.Release(target);
            for (int i = 0; i < m_Visible.Count; i++)
                m_Visible[i].MoveTo(SlotPosition(i), Catalog.ShiftDuration);

            PublishFill();
            FillAsync().Forget();
        }

        public void CollectSeated(List<CapsuleController> buffer)
        {
            for (int i = 0; i < m_Visible.Count; i++)
            {
                m_Visible[i].KillTweens();
                m_Visible[i].CollectSeated(buffer);
            }
        }

        public void Shutdown()
        {
            for (int i = 0; i < m_Visible.Count; i++)
                m_Factory.Release(m_Visible[i]);

            m_Visible.Clear();
            m_Factory.ReleasePooledInstances();
        }

        public void PublishFill()
        {
            if (m_Fills == null || m_Fills.Length != m_Visible.Count)
                m_Fills = new TargetFill[m_Visible.Count];

            for (int i = 0; i < m_Visible.Count; i++)
            {
                TargetController target = m_Visible[i];
                m_Fills[i] = new TargetFill(target.CapsuleColor, target.Occupied, target.Capacity);
            }

            int remaining = m_Queue.Length - m_NextIndex;
            EB.Presentation.Invoke(new RunTargetFillChanged(m_Fills, remaining));
        }

        private async UniTaskVoid FillAsync()
        {
            if (m_Filling)
                return;

            m_Filling = true;
            while (m_Visible.Count < Catalog.VisibleCount && m_NextIndex < m_Queue.Length)
            {
                TargetController target = await SpawnNext();
                if (target == null)
                    break;

                target.transform.localPosition = SlotPosition(ActiveCount);
                target.MoveTo(SlotPosition(m_Visible.Count - 1), Catalog.ShiftDuration);
                PublishFill();
            }

            m_Filling = false;
        }

        private async UniTask<TargetController> SpawnNext()
        {
            TargetQuota quota = m_Queue[m_NextIndex];
            TargetController target = await m_Factory.Create(quota.Capacity, m_Origin, m_Token);
            if (m_Token.IsCancellationRequested)
            {
                if (target != null)
                    m_Factory.Release(target);

                return null;
            }

            m_NextIndex++;
            if (target == null)
                return null;

            target.transform.localRotation = Quaternion.identity;
            target.Initialize(quota.Color, (int)quota.Capacity, m_FallbackExit);
            m_Visible.Add(target);
            return target;
        }

        private int ActiveCount => Mathf.Min(Catalog.VisibleCount, m_Visible.Count + m_Queue.Length - m_NextIndex);

        private Vector3 SlotPosition(int index)
        {
            return Vector3.right * (((ActiveCount - 1) * 0.5f - index) * Catalog.Spacing);
        }

        private static void HideExistingChildren(Transform origin)
        {
            for (int i = 0; i < origin.childCount; i++)
                origin.GetChild(i).gameObject.SetActive(false);
        }
    }
}
