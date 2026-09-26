using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class SpawnPacingSystem : ITickable
    {
        private readonly SpawnSystem m_Spawn;
        private readonly CapsuleSystem m_Capsules;
        private readonly IReadOnlyList<LevelPath> m_Paths;
        private readonly ILevelRunState m_Level;
        private readonly CancellationToken m_DestroyToken;
        private readonly float[] m_Timers;
        private readonly bool[] m_InFlight;

        private LevelDefinitionSO m_Definition;
        private float m_SpeedMultiplier = 1f;

        private bool HasRunEnded => m_Level.Phase == ELevelPhase.Complete || m_Level.Phase == ELevelPhase.Fail;

        public SpawnPacingSystem(
            SpawnSystem spawn,
            CapsuleSystem capsules,
            IReadOnlyList<LevelPath> paths,
            ILevelRunState level,
            CancellationToken destroyToken)
        {
            m_Spawn = spawn;
            m_Capsules = capsules;
            m_Paths = paths;
            m_Level = level;
            m_DestroyToken = destroyToken;
            m_Timers = new float[paths.Count];
            m_InFlight = new bool[paths.Count];

            EB.Gameplay.Add<RunStarted>(OnRunStarted);
            EB.Gameplay.Add<RunFinished>(OnRunFinished);
        }

        public void Shutdown()
        {
            EB.Gameplay.Remove<RunStarted>(OnRunStarted);
            EB.Gameplay.Remove<RunFinished>(OnRunFinished);
            m_SpeedMultiplier = 1f;
            m_Definition = null;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            m_SpeedMultiplier = multiplier <= 0f ? 1f : multiplier;
            if (m_Level.Phase == ELevelPhase.Playing)
                m_Capsules.SetPathSpeed(CurrentSpeed);
        }

        public void Tick(float deltaTime)
        {
            if (m_Level.Phase != ELevelPhase.Playing || m_Definition == null)
                return;

            m_Capsules.SetPathSpeed(CurrentSpeed);
            float interval = CurrentSpawnInterval;

            for (int i = 0; i < m_Paths.Count; i++)
            {
                if (m_InFlight[i])
                    continue;

                m_Timers[i] += deltaTime;
                if (m_Timers[i] < interval || m_Capsules.Count >= m_Definition.MaxActive)
                    continue;

                m_Timers[i] = 0f;
                SpawnAsync(i).Forget();
            }
        }

        private void OnRunStarted(RunStarted evt)
        {
            m_Definition = evt.Definition;
            m_SpeedMultiplier = 1f;

            float interval = m_Definition.SpawnInterval;
            for (int i = 0; i < m_Timers.Length; i++)
                m_Timers[i] = interval * (1f - i / (float)m_Timers.Length);
        }

        private void OnRunFinished(RunFinished evt)
        {
            m_Spawn.DespawnAll();
        }

        private async UniTaskVoid SpawnAsync(int pathIndex)
        {
            LevelPath path = m_Paths[pathIndex];
            if (!TryPickCapsule(path, out CapsuleDefinitionSO definition, out CapsuleColorSO color))
                return;

            m_InFlight[pathIndex] = true;
            CapsuleSpawnData data = new CapsuleSpawnData(definition, color, CurrentSpeed);
            await m_Spawn.Spawn(data, path, m_DestroyToken).SuppressCancellationThrow();
            if (!m_DestroyToken.IsCancellationRequested && HasRunEnded)
                m_Spawn.DespawnAll();

            m_InFlight[pathIndex] = false;
        }

        private bool TryPickCapsule(LevelPath path, out CapsuleDefinitionSO definition, out CapsuleColorSO color)
        {
            float poisonChance = m_Definition.PoisonDefinition != null ? m_Definition.PoisonChance : 0f;
            float goldChance = m_Definition.GoldDefinition != null ? m_Definition.GoldChance : 0f;
            float roll = Random.value;

            definition = roll < poisonChance ? m_Definition.PoisonDefinition
                : roll < poisonChance + goldChance ? m_Definition.GoldDefinition
                : m_Definition.NormalDefinition;

            if (definition == null)
            {
                color = null;
                Logger.Error("LevelDefinition has no normal capsule definition.");
                return false;
            }

            if (definition.Kind != ECapsuleKind.Normal)
            {
                color = definition.Color;
                return true;
            }

            if (path.TryPickColor(out color))
                return true;

            Logger.Error("Path has no color weights: " + path.name, path);
            return false;
        }

        private float CurrentBaseSpeed
        {
            get
            {
                float start = m_Definition.ConveyorSpeed;
                float max = m_Definition.MaxConveyorSpeed;
                return Mathf.Min(max, start + m_Level.Elapsed * m_Definition.SpeedRamp);
            }
        }

        private float CurrentSpeed => CurrentBaseSpeed * m_SpeedMultiplier;

        private float CurrentSpawnInterval
        {
            get
            {
                float startInterval = m_Definition.SpawnInterval;
                float startSpeed = m_Definition.ConveyorSpeed;
                float speedRange = m_Definition.MaxConveyorSpeed - startSpeed;
                if (speedRange <= 0f)
                    return startInterval;

                float progress = Mathf.Clamp01((CurrentBaseSpeed - startSpeed) / speedRange);
                return Mathf.Lerp(startInterval, m_Definition.MinSpawnInterval, progress);
            }
        }
    }
}
