using UnityEngine;
using UnityEngine.Serialization;

namespace PillFrenzy.Gameplay
{
    public sealed class LevelLayout : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private ConveyorPath m_Path;

        [Header("Camera")]
        [SerializeField] private Camera m_Camera;
        [SerializeField] private Transform m_ShakeHolder;

        [Header("Spawn Roots")]
        [FormerlySerializedAs("m_TargetRoot")]
        [SerializeField] private Transform m_TargetSpawnPoint;
        [SerializeField] private Transform m_CapsuleRoot;

        [Header("Anchors")]
        [SerializeField] private Transform m_TargetExit;

        [Header("Input")]
        [SerializeField] private LayerMask m_CapsuleMask = ~0;

        public IConveyorPath Path => m_Path;
        public Camera Camera => m_Camera;
        public Transform ShakeHolder => m_ShakeHolder; // NOTE(vsmsari): Since I don't use camera rig in this demo, it will be null.
        public Transform CapsuleRoot => m_CapsuleRoot;
        public Transform TargetSpawnPoint => m_TargetSpawnPoint;
        public Transform TargetExit => m_TargetExit;
        public LayerMask CapsuleMask => m_CapsuleMask;
    }
}
