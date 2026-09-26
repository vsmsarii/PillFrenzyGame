using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class CapsuleController : MonoBehaviour
    {
        private CapsuleDefinitionSO m_Definition;
        private CapsuleColorSO m_Color;
        private CapsuleView m_View;
        private IConveyorPath m_Path;
        private float m_Speed;
        private float m_Distance;
        private ECapsuleState m_State;
        private Tween m_FlyTween;
        [SerializeField] private float m_HeightOffset = 1.5f;

        public CapsuleDefinitionSO Definition => m_Definition;
        public CapsuleColorSO Color => m_Color;
        public ECapsuleState State => m_State;
        public bool HasReachedEnd =>
            m_State == ECapsuleState.OnPath
            && m_Path != null
            && m_Path.Length > 0f
            && m_Distance >= m_Path.Length;

        public void Initialize(CapsuleSpawnData data, IConveyorPath path)
        {
            KillFlight();
            m_Definition = data.Definition;
            m_Color = data.Color;
            m_Path = path;
            m_Speed = data.Speed;
            m_Distance = 0f;
            m_State = ECapsuleState.OnPath;
            SetColliderEnabled(true);
            ApplyPathPose();

            if (m_View == null)
                m_View = GetComponent<CapsuleView>();

            m_View.Initialize(m_Color.Color);
        }

        public void Tick(float deltaTime)
        {
            if (m_State != ECapsuleState.OnPath || m_Path == null)
                return;

            m_Distance += m_Speed * deltaTime;
            ApplyPathPose();
        }

        public void SetPathSpeed(float speed)
        {
            m_Speed = speed;
        }

        public void BeginFlight()
        {
            m_State = ECapsuleState.InFlight;
        }

        public void AttachToSlot(Transform slot, float seatOffset)
        {
            KillFlight();
            m_State = ECapsuleState.Seated;
            transform.SetParent(slot, true);
            transform.SetPositionAndRotation(slot.position + Vector3.up * seatOffset, slot.rotation);
            SetColliderEnabled(false);
        }

        public async UniTask FlyTo(Vector3 destination, Quaternion rotation, CancellationToken cancellationToken)
        {
            KillFlight();
            float duration = m_Definition != null ? m_Definition.FlyDuration : 0.35f;
            UniTaskCompletionSource source = new UniTaskCompletionSource();
            bool completed = false;

            Vector3 startPoint = transform.position;
            Vector3 midPoint = (startPoint + destination) / 2f + Vector3.up * m_HeightOffset;
            Vector3[] path = { startPoint, midPoint, destination };

            m_FlyTween = DOTween.Sequence()
            .Join(transform.DOPath(path, duration, PathType.CatmullRom).SetEase(Ease.OutQuad))
            .Join(transform.DORotateQuaternion(rotation, duration).SetEase(Ease.OutQuad))
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                completed = true;
                source.TrySetResult();
            })
            .OnKill(() =>
            {
                m_FlyTween = null;
                if (!completed)
                    source.TrySetCanceled();
            });

            using (cancellationToken.CanBeCanceled
                       ? cancellationToken.Register(KillFlight)
                       : default(CancellationTokenRegistration))
            {
                await source.Task.SuppressCancellationThrow();
            }
        }

        public void KillFlight()
        {
            if (m_FlyTween != null && m_FlyTween.IsActive())
                m_FlyTween.Kill();

            m_FlyTween = null;
        }

        private void ApplyPathPose()
        {
            Pose pose = m_Path.GetPose(m_Distance);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void SetColliderEnabled(bool enabled)
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = enabled;
        }
    }
}
