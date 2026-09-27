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
        private const float BakedSampleSpacing = 0.05f;

        [SerializeField] private List<PathColorWeight> m_Colors = new();

        private SplineContainer m_Container;
        private float m_Length;
        private float m_SampleSpacing;
        private Vector3[] m_SamplePositions;
        private Quaternion[] m_SampleRotations;

        public SplineContainer Container => m_Container != null ? m_Container : m_Container = GetComponent<SplineContainer>();
        public IReadOnlyList<PathColorWeight> Colors => m_Colors;
        public float Length => m_SamplePositions != null ? m_Length : Container.CalculateLength();
        public Vector3 StartPosition => Container.EvaluatePosition(0f);
        public Vector3 EndPosition => Container.EvaluatePosition(1f);

        private void Awake()
        {
            BakeSamples();
        }

        public Pose GetPose(float distance)
        {
            int lastIndex = m_SamplePositions.Length - 1;
            if (m_SampleSpacing <= 0f)
                return new Pose(m_SamplePositions[0], m_SampleRotations[0]);

            float samplePosition = Mathf.Clamp(distance / m_SampleSpacing, 0f, lastIndex);
            int index = Mathf.Min((int)samplePosition, lastIndex - 1);
            float blend = samplePosition - index;
            return new Pose(
                Vector3.LerpUnclamped(m_SamplePositions[index], m_SamplePositions[index + 1], blend),
                Quaternion.LerpUnclamped(m_SampleRotations[index], m_SampleRotations[index + 1], blend));
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

        private void BakeSamples()
        {
            m_Length = Container.CalculateLength();
            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(m_Length / BakedSampleSpacing) + 1);
            m_SampleSpacing = m_Length / (sampleCount - 1);
            m_SamplePositions = new Vector3[sampleCount];
            m_SampleRotations = new Quaternion[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                Container.Evaluate(i / (float)(sampleCount - 1), out float3 position, out float3 tangent, out float3 up);
                m_SamplePositions[i] = position;
                m_SampleRotations[i] = math.lengthsq(tangent) > 0f ? Quaternion.LookRotation(tangent, up) : transform.rotation;
            }
        }
    }
}
