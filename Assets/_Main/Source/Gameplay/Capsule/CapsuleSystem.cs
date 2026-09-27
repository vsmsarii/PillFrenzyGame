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
        private const float MaxTapDistance = 100f;
        private const float SpecialFlyHeight = 2f;

        private readonly IInputService m_Input;
        private readonly TargetSystem m_Targets;
        private readonly Camera m_Camera;
        private readonly CancellationToken m_DestroyToken;
        private readonly GameplayFeedback m_Feedback;
        private readonly List<CapsuleController> m_Capsules = new();
        private readonly List<CapsuleController> m_SeatedBuffer = new();
        private readonly RaycastHit[] m_Hits = new RaycastHit[MaxRaycastHits];
        private readonly List<RaycastResult> m_UiHits = new();
        private readonly int m_CapsuleLayerMask;
        private PointerEventData m_PointerData;
        private SpawnSystem m_Spawn;
        private ILevelRunState m_Level;
        private ICapsuleTapInterceptor m_TapInterceptor;
        private float m_PathSpeed;

        public int Count => m_Capsules.Count;
        public IReadOnlyList<CapsuleController> Active => m_Capsules;

        public CapsuleSystem(
            IInputService input,
            TargetSystem targets,
            Camera camera,
            CancellationToken destroyToken,
            GameplayFeedback feedback,
            int capsuleLayerMask)
        {
            m_Input = input;
            m_Targets = targets;
            m_Camera = camera;
            m_DestroyToken = destroyToken;
            m_Feedback = feedback;
            m_CapsuleLayerMask = capsuleLayerMask;
        }

        public void Bind(SpawnSystem spawn, ILevelRunState level)
        {
            m_Spawn = spawn;
            m_Level = level;
        }

        public void SetTapInterceptor(ICapsuleTapInterceptor interceptor)
        {
            m_TapInterceptor = interceptor;
        }

        public void Register(CapsuleController controller)
        {
            m_Capsules.Add(controller);
        }

        public void Unregister(CapsuleController controller)
        {
            m_Capsules.Remove(controller);
        }

        public void CopyActive(List<CapsuleController> buffer)
        {
            buffer.Clear();
            buffer.AddRange(m_Capsules);
        }

        public void Tick(float deltaTime)
        {
            HandleTap();

            if (!m_Level.IsSimulating)
                return;

            float distance = m_PathSpeed * deltaTime;
            for (int i = m_Capsules.Count - 1; i >= 0; i--)
            {
                CapsuleController capsule = m_Capsules[i];
                capsule.Advance(distance);
                if (capsule.HasReachedEnd)
                    m_Spawn.Despawn(capsule);
            }
        }

        public void SetPathSpeed(float speed)
        {
            m_PathSpeed = speed;
        }

        public void Shutdown()
        {
            m_Capsules.Clear();
        }

        private void HandleTap()
        {
            if (!m_Input.TryConsumeTap(out Vector2 screenPosition) || IsPointerOverUi(screenPosition))
                return;

            CapsuleController capsule = RaycastCapsule(screenPosition);
            if (m_TapInterceptor != null && m_TapInterceptor.TryIntercept(capsule))
                return;

            if (!m_Level.IsSimulating || capsule == null || capsule.State != ECapsuleState.OnPath)
                return;

            ECapsuleKind kind = capsule.Definition.Kind;
            if (kind == ECapsuleKind.Normal)
            {
                SendToMatchingTarget(capsule);
                return;
            }

            Vector3 destination = capsule.transform.position + Vector3.up * SpecialFlyHeight;
            FlySpecial(capsule, destination, kind).Forget();
        }

        private void SendToMatchingTarget(CapsuleController capsule)
        {
            if (!m_Targets.TryGet(capsule.Color, out TargetController target))
                return;

            if (!target.TryReserveSlot(out Transform slot, out int slotIndex))
                return;

            m_Targets.PublishFill();
            FlyNormal(capsule, target, slot, slotIndex).Forget();
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
            Ray ray = m_Camera.ScreenPointToRay(screenPosition);
            int hitCount = Physics.RaycastNonAlloc(ray, m_Hits, MaxTapDistance, m_CapsuleLayerMask);
            CapsuleController closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = m_Hits[i];
                if (hit.distance >= closestDistance)
                    continue;

                if (!hit.transform.TryGetComponent(out CapsuleController capsule))
                    continue;

                closest = capsule;
                closestDistance = hit.distance;
            }

            return closest;
        }

        private async UniTaskVoid FlyNormal(CapsuleController capsule, TargetController target, Transform slot, int slotIndex)
        {
            capsule.BeginFlight();
            await capsule.FlyTo(slot.position + Vector3.up * target.SeatOffset, slot.rotation, m_DestroyToken);
            await WaitWhilePaused();

            if (m_DestroyToken.IsCancellationRequested || m_Level.Phase != ELevelPhase.Playing)
            {
                target.CancelReserve(slotIndex);
                m_Targets.PublishFill();
                return;
            }

            m_Spawn.Detach(capsule);
            target.Seat(capsule, slotIndex);
            m_Targets.PublishFill();
            m_Feedback.PlayCorrect(slot.position, capsule.Color.Color);
            EB.Gameplay.Invoke(new CapsuleResolved(ECapsuleKind.Normal));

            if (target.IsFilled)
            {
                await target.PlayExit(m_DestroyToken);
                if (m_DestroyToken.IsCancellationRequested)
                    return;

                ReleaseSeated(target);
                m_Targets.Advance(target);
            }

            await WaitWhilePaused();
            if (m_DestroyToken.IsCancellationRequested)
                return;

            if (m_Level.Phase == ELevelPhase.Playing && m_Targets.IsComplete)
                EB.Gameplay.Invoke(new AllTargetsFilled());
        }

        private void ReleaseSeated(TargetController target)
        {
            m_SeatedBuffer.Clear();
            target.CollectSeated(m_SeatedBuffer);
            foreach (CapsuleController seated in m_SeatedBuffer)
                m_Spawn.Despawn(seated);

            m_SeatedBuffer.Clear();
        }

        private async UniTaskVoid FlySpecial(CapsuleController capsule, Vector3 destination, ECapsuleKind kind)
        {
            capsule.BeginFlight();
            await capsule.FlyTo(destination, capsule.transform.rotation, m_DestroyToken);
            await WaitWhilePaused();

            if (m_DestroyToken.IsCancellationRequested || m_Level.Phase != ELevelPhase.Playing)
                return;

            EB.Gameplay.Invoke(new CapsuleResolved(kind));

            if (kind == ECapsuleKind.Gold)
                m_Feedback.PlayGold(destination, capsule.Color.Color);
            else
                m_Feedback.PlayPoison(destination, capsule.Color.Color);

            if (m_Level.Phase == ELevelPhase.Playing)
                m_Spawn.Despawn(capsule);
        }

        private async UniTask WaitWhilePaused()
        {
            if (m_Level.IsPaused)
                await UniTask.WaitWhile(() => m_Level.IsPaused, cancellationToken: m_DestroyToken).SuppressCancellationThrow();
        }
    }
}
