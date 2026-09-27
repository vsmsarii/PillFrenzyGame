using System;
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
        private Transform m_Exit;
        private Tween m_ExitTween;
        private Tween m_MoveTween;
        private bool m_Filled;

        public CapsuleColorSO CapsuleColor => m_Color;
        public int Capacity => m_Capacity;
        public float SeatOffset => m_SeatOffset;
        public int Occupied => m_Filled ? m_Capacity : CountReserved();
        public bool IsFilled => m_Filled;
        public bool CanAccept => !m_Filled && Array.IndexOf(m_Reserved, false) >= 0;

        public void Initialize(CapsuleColorSO color, int capacity, Transform fallbackExit)
        {
            KillTweens();

            m_Color = color;
            m_Capacity = capacity;
            m_Filled = false;
            m_Exit = m_ExitPoint != null ? m_ExitPoint : fallbackExit;
            m_Seated = new CapsuleController[capacity];
            m_Reserved = new bool[capacity];

            m_View = GetComponent<TargetView>();
            m_View.Initialize(m_Color.Color);
        }

        public bool TryReserveSlot(out Transform slot, out int index)
        {
            index = m_Filled ? -1 : Array.IndexOf(m_Reserved, false);
            if (index < 0)
            {
                slot = null;
                return false;
            }

            m_Reserved[index] = true;
            slot = m_Slots[index];
            return true;
        }

        public void CancelReserve(int index)
        {
            m_Reserved[index] = false;
        }

        public void Seat(CapsuleController capsule, int index)
        {
            m_Seated[index] = capsule;
            capsule.AttachToSlot(m_Slots[index], m_SeatOffset);
            m_View.PlayLanded();
            m_Filled = Array.IndexOf(m_Seated, null) < 0;
        }

        public async UniTask PlayExit(CancellationToken cancellationToken)
        {
            KillTweens();
            m_ExitTween = transform
                .DOMove(m_Exit.position, m_ExitDuration)
                .SetEase(Ease.InOutQuad)
                .SetDelay(m_ExitDelay)
                .SetLink(gameObject);

            await m_ExitTween.WaitForEnd(cancellationToken);
        }

        public void CollectSeated(List<CapsuleController> buffer)
        {
            for (int i = 0; i < m_Seated.Length; i++)
            {
                if (m_Seated[i] == null)
                    continue;

                buffer.Add(m_Seated[i]);
                m_Seated[i] = null;
            }
        }

        public void MoveTo(Vector3 localPosition, float duration)
        {
            Kill(ref m_MoveTween);
            m_MoveTween = transform.DOLocalMove(localPosition, duration).SetEase(Ease.OutCubic).SetLink(gameObject);
        }

        public void KillTweens()
        {
            Kill(ref m_ExitTween);
            Kill(ref m_MoveTween);
        }

        private int CountReserved()
        {
            int count = 0;
            foreach (bool reserved in m_Reserved)
            {
                if (reserved)
                    count++;
            }

            return count;
        }

        private static void Kill(ref Tween tween)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();

            tween = null;
        }
    }
}
