using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class CapsuleController : MonoBehaviour
    {
        private CapsuleDefinitionSO m_Definition;
        private CapsuleView m_View;
        private IConveyorPath m_Path;
        private float m_Speed;
        private float m_Distance;
        private ECapsuleState m_State;
        private Tween m_FlyTween;
        [SerializeField] private float m_HeightOffset = 1.5f;

        public CapsuleDefinitionSO Definition => m_Definition;
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
            m_Path = path;
            m_Speed = data.Speed;
            m_Distance = data.StartDistance;
            m_State = ECapsuleState.OnPath;
            SetColliderEnabled(true);
            transform.position = m_Path.GetPoint(m_Distance);
        }

        public void ApplyPalette(ColorCatalogSO table)
        {
            if (m_View == null)
                m_View = GetComponent<CapsuleView>();

            if (m_View == null)
                m_View = gameObject.AddComponent<CapsuleView>();

            Color color = table != null && m_Definition != null
                ? table.Get(m_Definition.Color)
                : Color.white;

            m_View.Initialize(color);
        }

        public void Tick(float deltaTime)
        {
            if (m_State != ECapsuleState.OnPath || m_Path == null)
                return;

            m_Distance += m_Speed * deltaTime;
            transform.position = m_Path.GetPoint(m_Distance);
        }

        public void SetPathSpeed(float speed)
        {
            m_Speed = speed;
        }

        public void BeginFlight()
        {
            m_State = ECapsuleState.InFlight;
        }

        public void AttachToSlot(Transform slot)
        {
            KillFlight();
            m_State = ECapsuleState.Seated;
            transform.SetParent(slot, true);
            transform.localRotation = Quaternion.identity;
            SetColliderEnabled(false);
        }

        public async UniTask FlyTo(Vector3 destination, CancellationToken cancellationToken)
        {
            KillFlight();
            float duration = m_Definition != null ? m_Definition.FlyDuration : 0.35f;
            UniTaskCompletionSource source = new UniTaskCompletionSource();
            bool completed = false;

            Vector3 startPoint = transform.position;
            Vector3 midPoint = (startPoint + destination) / 2f + Vector3.up * m_HeightOffset;
            Vector3[] path = { startPoint, midPoint, destination };

            m_FlyTween = transform.DOPath(path, duration, PathType.CatmullRom).SetEase(Ease.OutQuad).SetLink(gameObject)
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

        private void SetColliderEnabled(bool enabled)
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = enabled;
        }
    }
}
