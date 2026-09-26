using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct PathColorWeight
    {
        [SerializeField] private CapsuleColorSO m_Color;
        [SerializeField, Min(0f)] private float m_Weight;

        public CapsuleColorSO Color => m_Color;
        public float Weight => m_Weight;

        public PathColorWeight(CapsuleColorSO color, float weight)
        {
            m_Color = color;
            m_Weight = weight;
        }
    }

    [RequireComponent(typeof(SplineContainer))]
    public sealed class LevelPath : MonoBehaviour, IConveyorPath
    {
        [SerializeField] private List<PathColorWeight> m_Colors = new();

        private SplineContainer m_Container;
        private float m_Length;

        public SplineContainer Container => m_Container != null ? m_Container : m_Container = GetComponent<SplineContainer>();
        public IReadOnlyList<PathColorWeight> Colors => m_Colors;
        public float Length => Application.isPlaying ? m_Length : Container.CalculateLength();
        public Vector3 StartPosition => Container.EvaluatePosition(0f);
        public Vector3 EndPosition => Container.EvaluatePosition(1f);

        private void Awake()
        {
            m_Length = Container.CalculateLength();
        }

        public Pose GetPose(float distance)
        {
            float t = m_Length > 0f ? Mathf.Clamp01(distance / m_Length) : 0f;
            Container.Evaluate(t, out float3 position, out float3 tangent, out float3 up);
            Quaternion rotation = math.lengthsq(tangent) > 0f ? Quaternion.LookRotation(tangent, up) : transform.rotation;
            return new Pose(position, rotation);
        }

        public bool TryPickColor(out CapsuleColorSO color)
        {
            float total = 0f;
            for (int i = 0; i < m_Colors.Count; i++)
            {
                if (m_Colors[i].Color != null)
                    total += m_Colors[i].Weight;
            }

            color = null;
            if (total <= 0f)
                return false;

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < m_Colors.Count; i++)
            {
                PathColorWeight entry = m_Colors[i];
                if (entry.Color == null || entry.Weight <= 0f)
                    continue;

                color = entry.Color;
                roll -= entry.Weight;
                if (roll <= 0f)
                    break;
            }

            return true;
        }

        public void SetColors(IEnumerable<PathColorWeight> colors)
        {
            m_Colors.Clear();
            m_Colors.AddRange(colors);
        }
    }
}
