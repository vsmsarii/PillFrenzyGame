using System;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class ConveyorPath : MonoBehaviour, IConveyorPath
    {
        [SerializeField] private Transform[] m_Waypoints;

        private float[] m_Cumulative;
        private float m_Length;

        public float Length => m_Length;

        private void Awake()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            if (m_Waypoints == null || m_Waypoints.Length == 0)
            {
                m_Cumulative = Array.Empty<float>();
                m_Length = 0f;
                return;
            }

            m_Cumulative = new float[m_Waypoints.Length];
            m_Cumulative[0] = 0f;

            for (int i = 1; i < m_Waypoints.Length; i++)
            {
                Transform from = m_Waypoints[i - 1];
                Transform to = m_Waypoints[i];
                if (from == null || to == null)
                {
                    Logger.Error("ConveyorPath has an unassigned waypoint at index " + i + ".", this);
                    m_Cumulative[i] = m_Cumulative[i - 1];
                    continue;
                }

                m_Cumulative[i] = m_Cumulative[i - 1] + Vector3.Distance(from.position, to.position);
            }

            m_Length = m_Cumulative[m_Cumulative.Length - 1];
        }

        public Vector3 GetPoint(float distance)
        {
            if (m_Waypoints == null || m_Waypoints.Length == 0)
                return transform.position;

            if (m_Cumulative == null)
                Rebuild();

            if (m_Waypoints.Length == 1 || m_Length <= 0f)
                return m_Waypoints[0].position;

            float d = Mathf.Clamp(distance, 0f, m_Length);

            for (int i = 1; i < m_Cumulative.Length; i++)
            {
                if (d > m_Cumulative[i])
                    continue;

                float span = m_Cumulative[i] - m_Cumulative[i - 1];
                float t = span <= 0f ? 0f : (d - m_Cumulative[i - 1]) / span;
                return Vector3.Lerp(m_Waypoints[i - 1].position, m_Waypoints[i].position, t);
            }

            return m_Waypoints[m_Waypoints.Length - 1].position;
        }

        private void OnDrawGizmos()
        {
            if (m_Waypoints == null || m_Waypoints.Length < 2)
                return;

            Gizmos.color = Color.cyan;
            for (int i = 1; i < m_Waypoints.Length; i++)
            {
                if (m_Waypoints[i - 1] == null || m_Waypoints[i] == null)
                    continue;

                Gizmos.DrawLine(m_Waypoints[i - 1].position, m_Waypoints[i].position);
            }
        }
    }
}
