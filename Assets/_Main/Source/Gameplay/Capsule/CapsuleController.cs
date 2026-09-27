using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class CapsuleController : MonoBehaviour
    {
        [SerializeField] private float m_HeightOffset = 1.5f;

        private CapsuleDefinitionSO m_Definition;
        private CapsuleColorSO m_Color;
        private CapsuleView m_View;
        private IConveyorPath m_Path;
        private Collider m_Collider;
        private float m_Distance;
        private readonly Vector3[] m_FlightPath = new Vector3[3];
        private ECapsuleState m_State;
        private Tween m_FlyTween;

        public CapsuleDefinitionSO Definition => m_Definition;
        public CapsuleColorSO Color => m_Color;
        public ECapsuleState State => m_State;
        public float Distance => m_Distance;
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
            m_Distance = 0f;
            m_State = ECapsuleState.OnPath;
            SetColliderEnabled(true);
            ApplyPathPose();

            if (m_View == null)
                m_View = GetComponent<CapsuleView>();

            m_View.Initialize(m_Color.Color);
        }

        public void Advance(float distance)
        {
            if (m_State != ECapsuleState.OnPath)
                return;

            m_Distance += distance;
            ApplyPathPose();
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

        public UniTask FlyTo(Vector3 destination, Quaternion rotation, CancellationToken cancellationToken)
        {
            KillFlight();
            float duration = m_Definition.FlyDuration;
            Vector3 start = transform.position;
            m_FlightPath[0] = start;
            m_FlightPath[1] = (start + destination) / 2f + Vector3.up * m_HeightOffset;
            m_FlightPath[2] = destination;

            m_FlyTween = DOTween.Sequence()
                .Join(transform.DOPath(m_FlightPath, duration, PathType.CatmullRom).SetEase(Ease.OutQuad))
                .Join(transform.DORotateQuaternion(rotation, duration).SetEase(Ease.OutQuad))
                .SetLink(gameObject);

            return m_FlyTween.WaitForEnd(cancellationToken);
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
            if (m_Collider == null)
                m_Collider = GetComponent<Collider>();

            m_Collider.enabled = enabled;
        }
    }
}
