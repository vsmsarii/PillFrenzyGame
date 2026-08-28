using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class LevelSystem : ITickable, ILevelRunState
    {
        private readonly SpawnSystem m_Spawn;
        private readonly CapsuleSystem m_Capsules;
        private readonly IConveyorPath m_Path;
        private readonly ISaveService m_Save;
        private readonly int m_LevelIndex;
        private readonly CancellationToken m_DestroyToken;
        private readonly GameplayFeedback m_Feedback;

        private LevelDefinitionSO m_Definition;
        private ELevelPhase m_Phase;
        private float m_Timer;
        private float m_Elapsed;
        private bool m_SpawnInFlight;
        private int m_Score;
        private int m_Combo;
        private int m_BestCombo;
        private int m_Health;
        private float m_SpeedMultiplier = 1f;

        public ELevelPhase Phase => m_Phase;
        public float Elapsed => m_Elapsed;
        public int LevelIndex => m_LevelIndex;

        private bool HasRunEnded => m_Phase == ELevelPhase.Complete || m_Phase == ELevelPhase.Fail;

        public LevelSystem(
            SpawnSystem spawn,
            CapsuleSystem capsules,
            IConveyorPath path,
            ISaveService save,
            int levelIndex,
            CancellationToken destroyToken,
            GameplayFeedback feedback)
        {
            m_Spawn = spawn;
            m_Capsules = capsules;
            m_Path = path;
            m_Save = save;
            m_LevelIndex = levelIndex;
            m_DestroyToken = destroyToken;
            m_Feedback = feedback;

            EB.Gameplay.Add<CapsuleResolved>(OnCapsuleResolved);
            EB.Gameplay.Add<AllTargetsFilled>(OnAllTargetsFilled);
        }

        private void OnCapsuleResolved(CapsuleResolved evt)
        {
            switch (evt.Kind)
            {
                case ECapsuleKind.Normal:
                    OnCorrect();
                    break;
                case ECapsuleKind.Gold:
                    OnGold();
                    break;
                case ECapsuleKind.Poison:
                    OnPoison();
                    break;
            }
        }

        private void OnAllTargetsFilled(AllTargetsFilled evt)
        {
            Complete();
        }

        public void StartRun(LevelDefinitionSO definition)
        {
            m_Definition = definition;
            m_Phase = ELevelPhase.Playing;
            m_Timer = definition.SpawnInterval;
            m_Elapsed = 0f;
            m_Score = 0;
            m_Combo = 0;
            m_BestCombo = 0;
            m_Health = definition.StartingHealth;
            m_SpeedMultiplier = 1f;

            if (m_Save != null)
                m_Save.IncrementLevelAttempts(m_LevelIndex);

            PublishHud();
            EB.Analytics.Invoke(new MatchStartAnalytics(m_LevelIndex));
        }

        public void Pause()
        {
            if (m_Phase == ELevelPhase.Playing)
                m_Phase = ELevelPhase.Paused;
        }

        public void Resume()
        {
            if (m_Phase == ELevelPhase.Paused)
                m_Phase = ELevelPhase.Playing;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            m_SpeedMultiplier = multiplier <= 0f ? 1f : multiplier;
            if (m_Phase == ELevelPhase.Playing)
                m_Capsules.SetPathSpeed(CurrentSpeed);
        }

        public void Tick(float deltaTime)
        {
            if (m_Phase != ELevelPhase.Playing || m_Definition == null)
                return;

            m_Elapsed += deltaTime;
            m_Capsules.SetPathSpeed(CurrentSpeed);

            if (m_SpawnInFlight)
                return;

            if (m_Capsules.Count >= m_Definition.MaxActive)
                return;

            m_Timer += deltaTime;
            if (m_Timer < CurrentSpawnInterval)
                return;

            m_Timer = 0f;
            SpawnAsync().Forget();
        }

        private void OnCorrect()
        {
            if (m_Phase != ELevelPhase.Playing || m_Definition == null)
                return;

            m_Combo++;
            if (m_Combo > m_BestCombo)
                m_BestCombo = m_Combo;

            m_Score += m_Definition.ScorePerCorrect * m_Combo;
            PublishHud();
        }

        private void OnGold()
        {
            if (m_Phase != ELevelPhase.Playing || m_Definition == null)
                return;

            int multiplier = m_Combo > 0 ? m_Combo : 1;
            m_Score += m_Definition.ScorePerCorrect * multiplier;
            PublishHud();
        }

        private void OnPoison()
        {
            if (m_Phase != ELevelPhase.Playing)
                return;

            m_Combo = 0;
            if (m_Save != null && m_Save.IsImmortalActive)
            {
                PublishHud();
                return;
            }

            m_Health--;
            PublishHud();

            if (m_Health <= 0)
                Fail();
        }

        private void Complete()
        {
            if (m_Phase != ELevelPhase.Playing)
                return;

            m_Phase = ELevelPhase.Complete;
            m_Spawn.DespawnAll();
            if (m_Save != null)
                m_Save.CompleteLevel(m_LevelIndex, m_Score, Mathf.RoundToInt(m_Elapsed));

            if (m_Feedback != null)
                m_Feedback.PlayComplete();
            EB.Analytics.Invoke(new MatchWinAnalytics(m_LevelIndex, m_Elapsed));
            EB.Presentation.Invoke(new RunEnded(true, m_Score, m_BestCombo));
        }

        private void Fail()
        {
            if (m_Phase != ELevelPhase.Playing)
                return;

            m_Phase = ELevelPhase.Fail;
            m_Spawn.DespawnAll();

            int attemptCount = 1;
            if (m_Save != null)
            {
                m_Save.TrySpendHeart();
                attemptCount = m_Save.GetLevelAttempts(m_LevelIndex);
            }

            if (m_Feedback != null)
                m_Feedback.PlayFail();
            EB.Analytics.Invoke(new MatchLoseAnalytics(m_LevelIndex, m_Elapsed, attemptCount));
            EB.Presentation.Invoke(new RunEnded(false, m_Score, m_BestCombo));
        }

        public void Shutdown()
        {
            EB.Gameplay.Remove<CapsuleResolved>(OnCapsuleResolved);
            EB.Gameplay.Remove<AllTargetsFilled>(OnAllTargetsFilled);
            m_SpeedMultiplier = 1f;
            m_Definition = null;
        }

        private async UniTaskVoid SpawnAsync()
        {
            m_SpawnInFlight = true;
            CapsuleDefinitionSO definition = PickDefinition();
            if (definition == null || m_Definition == null)
            {
                m_SpawnInFlight = false;
                return;
            }

            CapsuleSpawnData data = new CapsuleSpawnData(definition, 0f, CurrentSpeed);
            await m_Spawn.Spawn(data, m_Path, m_DestroyToken).SuppressCancellationThrow();
            if (!m_DestroyToken.IsCancellationRequested && HasRunEnded)
                m_Spawn.DespawnAll();

            m_SpawnInFlight = false;
        }

        private float CurrentBaseSpeed
        {
            get
            {
                if (m_Definition == null)
                    return 0f;

                float start = m_Definition.ConveyorSpeed;
                float max = m_Definition.MaxConveyorSpeed;
                return Mathf.Min(max, start + m_Elapsed * m_Definition.SpeedRamp);
            }
        }

        private float CurrentSpeed => CurrentBaseSpeed * m_SpeedMultiplier;

        private float CurrentSpawnInterval
        {
            get
            {
                if (m_Definition == null)
                    return 0f;

                float startInterval = m_Definition.SpawnInterval;
                float minInterval = m_Definition.MinSpawnInterval;
                float startSpeed = m_Definition.ConveyorSpeed;
                float maxSpeed = m_Definition.MaxConveyorSpeed;
                float speedRange = maxSpeed - startSpeed;
                if (speedRange <= 0f)
                    return startInterval;

                float progress = Mathf.Clamp01((CurrentBaseSpeed - startSpeed) / speedRange);
                return Mathf.Lerp(startInterval, minInterval, progress);
            }
        }

        private CapsuleDefinitionSO PickDefinition()
        {
            float poisonChance = m_Definition.PoisonDefinition != null ? m_Definition.PoisonChance : 0f;
            float goldChance = m_Definition.GoldDefinition != null ? m_Definition.GoldChance : 0f;
            float roll = UnityEngine.Random.value;

            if (roll < poisonChance)
                return m_Definition.PoisonDefinition;

            if (roll < poisonChance + goldChance)
                return m_Definition.GoldDefinition;

            CapsuleDefinitionSO[] definitions = m_Definition.CapsuleDefinitions;
            if (definitions == null || definitions.Length == 0)
            {
                Logger.Error("LevelDefinition has no capsule definitions.");
                return null;
            }

            return definitions[UnityEngine.Random.Range(0, definitions.Length)];
        }

        private void PublishHud()
        {
            EB.Presentation.Invoke(new RunHudChanged(m_Score, m_Combo, m_Health));
        }
    }
}
