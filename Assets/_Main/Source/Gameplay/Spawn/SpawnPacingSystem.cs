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

        private readonly Dictionary<int, ECapsuleKind> m_ForcedKinds = new();

        private LevelDefinitionSO m_Definition;
        private int m_SpawnCount;
        private float m_SpeedMultiplier = 1f;

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
        }

        public void ForceKind(ForcedCapsuleSpawn spawn)
        {
            if (!m_ForcedKinds.TryAdd(spawn.SpawnIndex, spawn.Kind) && m_ForcedKinds[spawn.SpawnIndex] != spawn.Kind)
                Logger.Warning("Spawn index " + spawn.SpawnIndex + " is already forced to " + m_ForcedKinds[spawn.SpawnIndex] + ". Ignoring " + spawn.Kind + ".");
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            m_SpeedMultiplier = multiplier;
        }

        public void ClearSpeedMultiplier()
        {
            SetSpeedMultiplier(1f);
        }

        public void Tick(float deltaTime)
        {
            if (!m_Level.IsSimulating)
                return;

            float baseSpeed = CurrentBaseSpeed;
            m_Capsules.SetPathSpeed(baseSpeed * m_SpeedMultiplier);
            float interval = SpawnIntervalAt(baseSpeed);

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
            m_SpawnCount = 0;
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
            CapsuleSpawnData data = new CapsuleSpawnData(definition, color);
            await m_Spawn.Spawn(data, path, m_DestroyToken).SuppressCancellationThrow();
            if (!m_DestroyToken.IsCancellationRequested && m_Level.HasEnded)
                m_Spawn.DespawnAll();

            m_InFlight[pathIndex] = false;
        }

        private bool TryPickCapsule(LevelPath path, out CapsuleDefinitionSO definition, out CapsuleColorSO color)
        {
            definition = PickDefinition(m_SpawnCount);
            if (definition.Kind != ECapsuleKind.Normal)
            {
                color = definition.Color;
                m_SpawnCount++;
                return true;
            }

            if (path.TryPickColor(out color))
            {
                m_SpawnCount++;
                return true;
            }

            Logger.Error("Path has no color weights: " + path.name, path);
            return false;
        }

        private CapsuleDefinitionSO PickDefinition(int spawnIndex)
        {
            if (m_ForcedKinds.TryGetValue(spawnIndex, out ECapsuleKind forcedKind))
            {
                CapsuleDefinitionSO forced = m_Definition.GetDefinition(forcedKind);
                if (forced != null)
                    return forced;

                Logger.Warning("Forced spawn " + spawnIndex + " wants " + forcedKind + " but the level has no definition for it.");
            }

            float poisonChance = m_Definition.PoisonDefinition != null ? m_Definition.PoisonChance : 0f;
            float goldChance = m_Definition.GoldDefinition != null ? m_Definition.GoldChance : 0f;
            float roll = Random.value;

            return roll < poisonChance ? m_Definition.PoisonDefinition
                : roll < poisonChance + goldChance ? m_Definition.GoldDefinition
                : m_Definition.NormalDefinition;
        }

        private float CurrentBaseSpeed => Mathf.Min(
            m_Definition.MaxConveyorSpeed,
            m_Definition.ConveyorSpeed + m_Level.Elapsed * m_Definition.SpeedRamp);

        private float SpawnIntervalAt(float baseSpeed)
        {
            float startInterval = m_Definition.SpawnInterval;
            float startSpeed = m_Definition.ConveyorSpeed;
            float speedRange = m_Definition.MaxConveyorSpeed - startSpeed;
            if (speedRange <= 0f)
                return startInterval;

            float progress = Mathf.Clamp01((baseSpeed - startSpeed) / speedRange);
            return Mathf.Lerp(startInterval, m_Definition.MinSpawnInterval, progress);
        }
    }
}
