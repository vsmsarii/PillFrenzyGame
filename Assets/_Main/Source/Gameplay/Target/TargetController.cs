using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [RequireComponent(typeof(TargetView))]
    public sealed class TargetController : MonoBehaviour
    {
        [SerializeField] private Transform[] m_Slots;
        [SerializeField] private Transform m_ExitPoint;
        [SerializeField] private float m_ExitDuration = 0.5f;
        [SerializeField] private float m_ExitDelay = 0.5f;
        [SerializeField] private float m_SeatOffset = 0.7f;

        private CapsuleColorSO m_Color;
        private int m_Capacity;
        private TargetView m_View;
        private CapsuleController[] m_Seated;
        private bool[] m_Reserved;
        private Transform m_ResolvedExit;
        private Tween m_ExitTween;
        private Tween m_MoveTween;
        private bool m_Filled;

        public CapsuleColorSO CapsuleColor => m_Color;
        public int Capacity => m_Capacity;
        public float SeatOffset => m_SeatOffset;
        public int Occupied
        {
            get
            {
                if (m_Filled)
                    return m_Capacity;

                if (m_Seated == null || m_Reserved == null)
                    return 0;

                int count = 0;
                for (int i = 0; i < m_Capacity; i++)
                {
                    if (m_Seated[i] != null || m_Reserved[i])
                        count++;
                }

                return count;
            }
        }
        public bool IsFilled => m_Filled;
        public bool CanAccept => !m_Filled && HasFreeSlot();

        public void Initialize(CapsuleColorSO color, int capacity, Transform fallbackExit)
        {
            KillTweens();

            m_Color = color;
            m_Filled = false;
            m_ResolvedExit = m_ExitPoint != null ? m_ExitPoint : fallbackExit;
            if (m_ResolvedExit == null)
                Logger.Error("Target has no exit point.", this);
            m_Capacity = ResolveCapacity(capacity);
            m_Seated = new CapsuleController[m_Capacity];
            m_Reserved = new bool[m_Capacity];

            m_View = GetComponent<TargetView>();
            m_View.Initialize(m_Color.Color);
        }

        public bool TryReserveSlot(out Transform slot, out int index)
        {
            slot = null;
            index = -1;
            if (!CanAccept || m_Slots == null)
                return false;

            for (int i = 0; i < m_Capacity; i++)
            {
                if (m_Slots[i] == null || m_Reserved[i] || m_Seated[i] != null)
                    continue;

                m_Reserved[i] = true;
                slot = m_Slots[i];
                index = i;
                return true;
            }

            return false;
        }

        public void CancelReserve(int index)
        {
            if (m_Reserved == null || index < 0 || index >= m_Reserved.Length)
                return;

            if (m_Seated[index] == null)
                m_Reserved[index] = false;
        }

        public void Seat(CapsuleController capsule, int index)
        {
            if (capsule == null || m_Slots == null || index < 0 || index >= m_Capacity)
                return;

            Transform slot = m_Slots[index];
            if (slot == null)
                return;

            m_Reserved[index] = true;
            m_Seated[index] = capsule;
            capsule.AttachToSlot(slot, m_SeatOffset);
            if (m_View != null)
                m_View.PlayLanded();

            if (!HasEmptySeat())
                m_Filled = true;
        }

        public async UniTask PlayExit(CancellationToken cancellationToken)
        {
            KillTweens();
            if (m_ResolvedExit == null)
                return;

            Vector3 destination = m_ResolvedExit.position;

            UniTaskCompletionSource source = new UniTaskCompletionSource();
            bool completed = false;
            m_ExitTween = transform
                .DOMove(destination, m_ExitDuration)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject)
                .SetDelay(m_ExitDelay)
                .OnComplete(() =>
                {
                    completed = true;
                    source.TrySetResult();
                })
                .OnKill(() =>
                {
                    m_ExitTween = null;
                    if (!completed)
                        source.TrySetCanceled();
                });

            using (cancellationToken.CanBeCanceled
                       ? cancellationToken.Register(KillExit)
                       : default(CancellationTokenRegistration))
            {
                await source.Task.SuppressCancellationThrow();
            }
        }

        public void CollectSeated(List<CapsuleController> buffer)
        {
            if (m_Seated == null)
                return;

            for (int i = 0; i < m_Seated.Length; i++)
            {
                CapsuleController seated = m_Seated[i];
                if (seated == null)
                    continue;

                buffer.Add(seated);
                m_Seated[i] = null;
            }
        }

        public void MoveTo(Vector3 localPosition, float duration)
        {
            if (m_MoveTween != null && m_MoveTween.IsActive())
                m_MoveTween.Kill();

            m_MoveTween = transform.DOLocalMove(localPosition, duration).SetEase(Ease.OutCubic).SetLink(gameObject);
        }

        public void KillTweens()
        {
            KillExit();
            if (m_MoveTween != null && m_MoveTween.IsActive())
                m_MoveTween.Kill();

            m_MoveTween = null;
        }

        private void KillExit()
        {
            if (m_ExitTween != null && m_ExitTween.IsActive())
                m_ExitTween.Kill();

            m_ExitTween = null;
        }

        private bool HasFreeSlot()
        {
            if (m_Slots == null || m_Reserved == null)
                return false;

            for (int i = 0; i < m_Capacity; i++)
            {
                if (m_Slots[i] != null && !m_Reserved[i] && m_Seated[i] == null)
                    return true;
            }

            return false;
        }

        private bool HasEmptySeat()
        {
            if (m_Seated == null)
                return true;

            for (int i = 0; i < m_Capacity; i++)
            {
                if (m_Slots[i] == null)
                    continue;

                if (m_Seated[i] == null)
                    return true;
            }

            return false;
        }

        private int ResolveCapacity(int capacity)
        {
            int available = m_Slots != null ? m_Slots.Length : 0;
            if (capacity <= 0 || capacity > available)
            {
                Logger.Error("Target capacity does not match prefab slot count.");
                return available;
            }

            return capacity;
        }
    }
}
