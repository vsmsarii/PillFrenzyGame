using System.Collections.Generic;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class LevelCameraFramer : ILateTickable
    {
        private const int SamplesPerPath = 32;

        private readonly Camera m_Camera;
        private readonly Transform m_Rig;
        private readonly LevelLayout m_Layout;
        private readonly LevelDefinitionSO m_Definition;
        private readonly GlobalSettingsSO m_Settings;
        private readonly TargetCatalogSO m_TargetCatalog;
        private readonly List<Vector3> m_Samples = new();

        public LevelCameraFramer(
            Camera camera,
            LevelLayout layout,
            LevelDefinitionSO definition,
            GlobalSettingsSO settings,
            TargetCatalogSO targetCatalog)
        {
            m_Camera = camera;
            m_Rig = camera.transform.parent;
            m_Layout = layout;
            m_Definition = definition;
            m_Settings = settings;
            m_TargetCatalog = targetCatalog;

            CollectSamples();
            LateTick(0f);
        }

        public void LateTick(float deltaTime)
        {
            Quaternion rotation = m_Definition.CameraRotation;
            m_Camera.fieldOfView = m_Definition.FieldOfView;

            float tanV = Mathf.Tan(m_Definition.FieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * m_Camera.aspect;
            float tanTop = (1f - 2f * m_Settings.HudTopInset) * tanV;
            float tanBottom = (2f * m_Settings.BoxRowTopInset - 1f) * tanV;

            Vector3 local = FitContent(Quaternion.Inverse(rotation), tanH, tanTop, tanBottom);
            m_Rig.SetPositionAndRotation(rotation * local, rotation);
            PlaceBoxRow(rotation, tanV, tanH);
        }

        private Vector3 FitContent(Quaternion inverse, float tanH, float tanTop, float tanBottom)
        {
            float padding = m_Definition.FramingPadding;
            float horizontalPad = padding * Mathf.Sqrt(1f + tanH * tanH);
            float topPad = padding * Mathf.Sqrt(1f + tanTop * tanTop);
            float bottomPad = padding * Mathf.Sqrt(1f + tanBottom * tanBottom);

            float maxA = float.MinValue, minB = float.MaxValue, maxC = float.MinValue, minD = float.MaxValue;
            for (int i = 0; i < m_Samples.Count; i++)
            {
                Vector3 p = inverse * m_Samples[i];
                maxA = Mathf.Max(maxA, p.x - tanH * p.z + horizontalPad);
                minB = Mathf.Min(minB, p.x + tanH * p.z - horizontalPad);
                maxC = Mathf.Max(maxC, p.y - tanTop * p.z + topPad);
                minD = Mathf.Min(minD, p.y - tanBottom * p.z - bottomPad);
            }

            float fitHorizontal = (minB - maxA) / (2f * tanH);
            float fitVertical = (minD - maxC) / (tanTop - tanBottom);
            float z = Mathf.Min(fitHorizontal, fitVertical) - m_Definition.CameraDistance;

            float x = (maxA + minB) * 0.5f;
            float y = ((maxC + tanTop * z) + (minD + tanBottom * z)) * 0.5f;
            return new Vector3(x, y, z);
        }

        private void PlaceBoxRow(Quaternion rotation, float tanV, float tanH)
        {
            float rowWidth = m_TargetCatalog.Spacing * m_TargetCatalog.VisibleCount + m_Settings.BoxRowSideMargin * 2f;
            float depth = Mathf.Max(m_Settings.BoxRowMinDepth, rowWidth * 0.5f / tanH);
            float height = (2f * m_Settings.BoxRowViewportY - 1f) * tanV * depth;

            Transform anchor = m_Layout.TargetSpawnPoint;
            anchor.SetPositionAndRotation(
                m_Rig.position + rotation * new Vector3(0f, height, depth),
                Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
        }

        private void CollectSamples()
        {
            IReadOnlyList<LevelPath> paths = m_Layout.Paths;
            for (int p = 0; p < paths.Count; p++)
            {
                for (int i = 0; i <= SamplesPerPath; i++)
                    m_Samples.Add(paths[p].Container.EvaluatePosition(i / (float)SamplesPerPath));
            }
        }
    }
}
