using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PillFrenzy.Gameplay
{
    public sealed class CapsuleSystem : ITickable
    {
        private const int MaxRaycastHits = 8;

        private readonly IInputService m_Input;
        private readonly TargetSystem m_Targets;
        private readonly Camera m_Camera;
        private readonly CancellationToken m_DestroyToken;
        private readonly ColorCatalogSO m_ColorTable;
        private readonly GameplayFeedback m_Feedback;
        private readonly List<CapsuleController> m_Capsules = new();
        private readonly List<CapsuleController> m_SeatedBuffer = new();
        private readonly RaycastHit[] m_Hits = new RaycastHit[MaxRaycastHits];
        private readonly List<RaycastResult> m_UiHits = new List<RaycastResult>(8);
        private readonly int m_CapsuleLayerMask;
        private PointerEventData m_PointerData;
        private SpawnSystem m_Spawn;
        private ILevelRunState m_Level;

        public int Count => m_Capsules.Count;

        public CapsuleSystem(
            IInputService input,
            TargetSystem targets,
            Camera camera,
            CancellationToken destroyToken,
            ColorCatalogSO colorTable,
            GameplayFeedback feedback,
            int capsuleLayerMask)
        {
            m_Input = input;
            m_Targets = targets;
            m_Camera = camera;
            m_DestroyToken = destroyToken;
            m_ColorTable = colorTable;
            m_Feedback = feedback;
            m_CapsuleLayerMask = capsuleLayerMask;
        }

        public void Bind(SpawnSystem spawn, ILevelRunState level)
        {
            m_Spawn = spawn;
            m_Level = level;
        }

        public void Register(CapsuleController controller)
        {
            if (controller == null)
                return;

            controller.ApplyPalette(m_ColorTable);
            m_Capsules.Add(controller);
        }

        public void Unregister(CapsuleController controller)
        {
            m_Capsules.Remove(controller);
        }

        public void CopyActive(List<CapsuleController> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < m_Capsules.Count; i++)
                buffer.Add(m_Capsules[i]);
        }

        public void Tick(float deltaTime)
        {
            TryHandleTap();

            if (m_Level == null || m_Level.Phase != ELevelPhase.Playing)
                return;

            for (int i = m_Capsules.Count - 1; i >= 0; i--)
            {
                CapsuleController controller = m_Capsules[i];
                if (controller == null)
                {
                    m_Capsules.RemoveAt(i);
                    continue;
                }

                controller.Tick(deltaTime);

                if (controller.HasReachedEnd)
                    m_Spawn.Despawn(controller);
            }
        }

        public void SetPathSpeed(float speed)
        {
            for (int i = 0; i < m_Capsules.Count; i++)
            {
                CapsuleController controller = m_Capsules[i];
                if (controller != null && controller.State == ECapsuleState.OnPath)
                    controller.SetPathSpeed(speed);
            }
        }

        public void Shutdown()
        {
            m_Capsules.Clear();
        }

        private void TryHandleTap()
        {
            if (!m_Input.TryConsumeTap(out Vector2 screenPosition))
                return;

            if (IsPointerOverUi(screenPosition))
                return;

            if (m_Level == null || m_Level.Phase != ELevelPhase.Playing)
                return;

            CapsuleController capsule = RaycastCapsule(screenPosition);
            if (capsule == null || capsule.State != ECapsuleState.OnPath || capsule.Definition == null)
                return;

            ECapsuleKind kind = capsule.Definition.Kind;
            if (kind == ECapsuleKind.Normal)
            {
                if (!m_Targets.TryGet(capsule.Definition.Color, out TargetController target))
                    return;

                if (!target.TryReserveSlot(out Transform slot, out int slotIndex))
                    return;

                m_Targets.PublishFill();
                FlyNormal(capsule, target, slot, slotIndex).Forget();
                return;
            }

            if (kind == ECapsuleKind.Gold)
            {
                FlySpecial(capsule, OffsetSpecialFly(capsule.transform.position), ECapsuleKind.Gold).Forget();
                return;
            }

            if (kind == ECapsuleKind.Poison)
                FlySpecial(capsule, OffsetSpecialFly(capsule.transform.position), ECapsuleKind.Poison).Forget();
        }

        private bool IsPointerOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (m_PointerData == null || m_PointerData.currentInputModule != eventSystem.currentInputModule)
                m_PointerData = new PointerEventData(eventSystem);

            m_PointerData.position = screenPosition;
            m_UiHits.Clear();
            eventSystem.RaycastAll(m_PointerData, m_UiHits);
            return m_UiHits.Count > 0;
        }

        private CapsuleController RaycastCapsule(Vector2 screenPosition)
        {
            if (m_Camera == null)
                return null;

            Ray ray = m_Camera.ScreenPointToRay(screenPosition);
            int hitCount = Physics.RaycastNonAlloc(ray, m_Hits, 100f, m_CapsuleLayerMask);
            CapsuleController best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = m_Hits[i];
                if (hit.distance >= bestDistance)
                    continue;

                CapsuleController capsule = hit.collider.GetComponentInParent<CapsuleController>();
                if (capsule == null)
                    continue;

                best = capsule;
                bestDistance = hit.distance;
            }

            return best;
        }

        private async UniTaskVoid FlyNormal(CapsuleController capsule, TargetController target, Transform slot, int slotIndex)
        {
            capsule.BeginFlight();
            await capsule.FlyTo(slot.position + Vector3.up * target.SeatOffset, m_DestroyToken);
            await WaitWhilePaused();

            bool aborted = m_DestroyToken.IsCancellationRequested
                || m_Level == null
                || m_Level.Phase != ELevelPhase.Playing
                || capsule == null
                || target == null;

            if (aborted)
            {
                if (target != null)
                    target.CancelReserve(slotIndex);

                m_Targets.PublishFill();
                return;
            }

            m_Spawn.Detach(capsule);
            target.Seat(capsule, slotIndex);
            m_Targets.PublishFill();
            if (m_Feedback != null)
                m_Feedback.PlayCorrect(slot.position);

            EB.Gameplay.Invoke(new CapsuleResolved(ECapsuleKind.Normal));

            if (target.IsFilled)
            {
                await target.PlayExit(m_DestroyToken);
                if (m_DestroyToken.IsCancellationRequested)
                    return;

                ReleaseSeated(target);
            }

            if (m_Level.Phase == ELevelPhase.Playing && m_Targets.AreAllFull())
                EB.Gameplay.Invoke(new AllTargetsFilled());
        }

        private void ReleaseSeated(TargetController target)
        {
            m_SeatedBuffer.Clear();
            target.CollectSeated(m_SeatedBuffer);
            for (int i = 0; i < m_SeatedBuffer.Count; i++)
                m_Spawn.Despawn(m_SeatedBuffer[i]);

            m_SeatedBuffer.Clear();
            m_Targets.PublishFill();
        }

        private async UniTaskVoid FlySpecial(CapsuleController capsule, Vector3 destination, ECapsuleKind kind)
        {
            capsule.BeginFlight();
            await capsule.FlyTo(destination, m_DestroyToken);
            await WaitWhilePaused();

            if (m_DestroyToken.IsCancellationRequested || capsule == null || m_Level == null)
                return;

            if (m_Level.Phase != ELevelPhase.Playing)
                return;

            EB.Gameplay.Invoke(new CapsuleResolved(kind));

            if (m_Feedback != null)
            {
                if (kind == ECapsuleKind.Gold)
                    m_Feedback.PlayGold(destination);
                else
                    m_Feedback.PlayPoison(destination);
            }

            if (m_Level.Phase == ELevelPhase.Playing)
                m_Spawn.Despawn(capsule);
        }

        private async UniTask WaitWhilePaused()
        {
            if (m_Level == null || m_Level.Phase != ELevelPhase.Paused)
                return;

            await UniTask.WaitWhile(
                () => m_Level != null && m_Level.Phase == ELevelPhase.Paused,
                cancellationToken: m_DestroyToken).SuppressCancellationThrow();
        }

        private Vector3 OffsetSpecialFly(Vector3 sourcePosition)
        {
            return sourcePosition + Vector3.up * 2f;
        }
    }
}
