using PillFrenzy.Gameplay;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace PillFrenzy.Editor
{
    public static class LevelPathGizmos
    {
        private const float MarkerRadius = 0.25f;
        private const float ArrowSpacing = 1.5f;
        private const float ArrowSize = 0.35f;

        private static readonly Color s_StartColor = new(0.3f, 0.9f, 0.4f);
        private static readonly Color s_EndColor = new(0.95f, 0.35f, 0.3f);
        private static readonly Color s_FlowColor = new(1f, 1f, 1f, 0.7f);

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        private static void Draw(LevelPath path, GizmoType gizmoType)
        {
            if (path.Container.Spline == null || path.Container.Spline.Count < 2)
                return;

            Vector3 start = path.StartPosition;
            Vector3 end = path.EndPosition;

            Gizmos.color = s_StartColor;
            Gizmos.DrawSphere(start, MarkerRadius);
            Gizmos.color = s_EndColor;
            Gizmos.DrawSphere(end, MarkerRadius);

            Handles.Label(start + Vector3.up * 0.5f, path.name + " Start");
            Handles.Label(end + Vector3.up * 0.5f, path.name + " End");

            DrawFlow(path);
        }

        private static void DrawFlow(LevelPath path)
        {
            float length = path.Length;
            int arrows = Mathf.FloorToInt(length / ArrowSpacing);
            Handles.color = s_FlowColor;

            for (int i = 1; i <= arrows; i++)
            {
                float t = i * ArrowSpacing / length;
                path.Container.Evaluate(t, out float3 position, out float3 tangent, out float3 up);
                if (math.lengthsq(tangent) <= 0f)
                    continue;

                Vector3 forward = math.normalize(tangent);
                Vector3 side = Vector3.Cross(up, forward).normalized * ArrowSize * 0.6f;
                Vector3 tip = (Vector3)position + forward * ArrowSize;
                Vector3 tail = (Vector3)position - forward * ArrowSize;
                Handles.DrawLine(tail + side, tip);
                Handles.DrawLine(tail - side, tip);
            }
        }
    }
}
