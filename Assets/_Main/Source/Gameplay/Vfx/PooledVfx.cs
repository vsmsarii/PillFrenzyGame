using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class PooledVfx : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float m_MaxLifetime = 3f;

        private ParticleSystem[] m_Systems;
        private ParticleSystem.MinMaxGradient[] m_OriginalColors;
        private float m_Elapsed;

        public void Play(Color? tint)
        {
            if (m_Systems == null)
                CacheSystems();

            m_Elapsed = 0f;
            for (int i = 0; i < m_Systems.Length; i++)
            {
                ParticleSystem.MainModule main = m_Systems[i].main;
                main.startColor = tint.HasValue ? Tint(m_OriginalColors[i], tint.Value) : m_OriginalColors[i];
                m_Systems[i].Clear(false);
                m_Systems[i].Play(false);
            }
        }

        public bool Tick(float deltaTime)
        {
            m_Elapsed += deltaTime;
            if (m_Elapsed >= m_MaxLifetime)
                return false;

            if (m_Systems.Length == 0)
                return true;

            for (int i = 0; i < m_Systems.Length; i++)
            {
                if (m_Systems[i].IsAlive(false))
                    return true;
            }

            return false;
        }

        public void Stop()
        {
            if (m_Systems == null)
                return;

            for (int i = 0; i < m_Systems.Length; i++)
                m_Systems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void CacheSystems()
        {
            m_Systems = GetComponentsInChildren<ParticleSystem>(true);
            m_OriginalColors = new ParticleSystem.MinMaxGradient[m_Systems.Length];
            for (int i = 0; i < m_Systems.Length; i++)
            {
                ParticleSystem.MainModule main = m_Systems[i].main;
                main.playOnAwake = false;
                main.stopAction = ParticleSystemStopAction.None;
                m_OriginalColors[i] = main.startColor;
            }
        }

        private static ParticleSystem.MinMaxGradient Tint(ParticleSystem.MinMaxGradient original, Color tint)
        {
            switch (original.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(original.color * tint);
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(original.colorMin * tint, original.colorMax * tint);
                default:
                    return new ParticleSystem.MinMaxGradient(tint);
            }
        }
    }
}
