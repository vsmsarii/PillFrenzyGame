using UnityEngine;
using UnityEngine.Splines;

namespace PillFrenzy.Gameplay
{
    [ExecuteAlways]
    [RequireComponent(typeof(SplineContainer), typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PathMesh : MonoBehaviour
    {
        [Header("Mesh Slots")]
        [SerializeField] private Mesh m_SegmentMesh;
        [SerializeField] private Mesh m_StartMesh;
        [SerializeField] private Mesh m_EndMesh;

        [Header("Procedural")]
        [SerializeField, Min(0.05f)] private float m_Width = 1f;
        [SerializeField, Min(0.01f)] private float m_Thickness = 1f;
        [SerializeField, Min(1f)] private float m_SamplesPerUnit = 4f;
        [SerializeField, Min(0.01f)] private float m_UvTileLength = 1f;

        [Header("Placement")]
        [SerializeField] private float m_SurfaceOffset = -0.5f;

        private readonly PathMeshBuilder m_Builder = new();
        private SplineContainer m_Container;
        private Mesh m_Mesh;

        private void OnEnable()
        {
            m_Container = GetComponent<SplineContainer>();
            Rebuild();
#if UNITY_EDITOR
            Spline.Changed += OnSplineChanged;
#endif
        }

        private void OnDestroy()
        {
            if (m_Mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(m_Mesh);
            else
                DestroyImmediate(m_Mesh);
        }

        public void Rebuild()
        {
            if (m_Container == null || m_Container.Spline == null)
                return;

            if (m_Mesh == null)
            {
                m_Mesh = new Mesh { name = name + " Mesh", hideFlags = HideFlags.DontSave };
                GetComponent<MeshFilter>().sharedMesh = m_Mesh;
            }

            m_Builder.Build(m_Container.Spline, new PathMeshSettings
            {
                SegmentMesh = m_SegmentMesh,
                StartMesh = m_StartMesh,
                EndMesh = m_EndMesh,
                Width = m_Width,
                Thickness = m_Thickness,
                SurfaceOffset = m_SurfaceOffset,
                SamplesPerUnit = m_SamplesPerUnit,
                UvTileLength = m_UvTileLength
            }, m_Mesh);
        }

#if UNITY_EDITOR
        private bool m_Dirty;

        private void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        private void OnValidate()
        {
            MarkDirty();
        }

        private void Update()
        {
            if (!m_Dirty)
                return;

            m_Dirty = false;
            Rebuild();
        }

        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
        {
            if (m_Container != null && spline == m_Container.Spline)
                MarkDirty();
        }

        private void MarkDirty()
        {
            m_Dirty = true;
            if (!Application.isPlaying)
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
    }
}
