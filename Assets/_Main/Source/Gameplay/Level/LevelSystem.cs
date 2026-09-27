using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class LevelSystem : ITickable, ILevelRunState
    {
        private readonly RunScoreSystem m_Score;
        private readonly ISaveService m_Save;
        private readonly int m_LevelIndex;
        private readonly GameplayFeedback m_Feedback;

        private ELevelPhase m_Phase;
        private EPauseReason m_PauseReasons;
        private float m_Elapsed;

        public ELevelPhase Phase => m_Phase;
        public bool IsPaused => m_PauseReasons != EPauseReason.None;
        public bool IsSimulating => m_Phase == ELevelPhase.Playing && !IsPaused;
        public bool HasEnded => m_Phase == ELevelPhase.Complete || m_Phase == ELevelPhase.Fail;
        public float Elapsed => m_Elapsed;
        public int LevelIndex => m_LevelIndex;

        public LevelSystem(
            RunScoreSystem score,
            ISaveService save,
            int levelIndex,
            GameplayFeedback feedback)
        {
            m_Score = score;
            m_Save = save;
            m_LevelIndex = levelIndex;
            m_Feedback = feedback;

            EB.Gameplay.Add<AllTargetsFilled>(OnAllTargetsFilled);
            EB.Gameplay.Add<RunHealthDepleted>(OnRunHealthDepleted);
        }

        public void EnterIntro()
        {
            TryEnter(ELevelPhase.Intro);
        }

        public bool StartRun(LevelDefinitionSO definition)
        {
            if (!TryEnter(ELevelPhase.Playing))
                return false;

            m_Elapsed = 0f;
            m_Save.IncrementLevelAttempts(m_LevelIndex);

            EB.Gameplay.Invoke(new RunStarted(definition));
            EB.Analytics.Invoke(new MatchStartAnalytics(m_LevelIndex));
            return true;
        }

        public void Pause(EPauseReason reason)
        {
            m_PauseReasons |= reason;
        }

        public void Resume(EPauseReason reason)
        {
            m_PauseReasons &= ~reason;
        }

        public void Tick(float deltaTime)
        {
            if (!IsSimulating)
                return;

            m_Elapsed += deltaTime;
        }

        public void Shutdown()
        {
            EB.Gameplay.Remove<AllTargetsFilled>(OnAllTargetsFilled);
            EB.Gameplay.Remove<RunHealthDepleted>(OnRunHealthDepleted);
        }

        private bool TryEnter(ELevelPhase next)
        {
            if (!IsAllowed(m_Phase, next))
            {
                Logger.Warning("Level phase transition rejected: " + m_Phase + " -> " + next);
                return false;
            }

            m_Phase = next;
            return true;
        }

        private static bool IsAllowed(ELevelPhase from, ELevelPhase to)
        {
            return (from, to) switch
            {
                (ELevelPhase.None, ELevelPhase.Intro) => true,
                (ELevelPhase.None, ELevelPhase.Playing) => true,
                (ELevelPhase.Intro, ELevelPhase.Playing) => true,
                (ELevelPhase.Playing, ELevelPhase.Complete) => true,
                (ELevelPhase.Playing, ELevelPhase.Fail) => true,
                _ => false
            };
        }

        private void OnAllTargetsFilled(AllTargetsFilled evt)
        {
            Complete();
        }

        private void OnRunHealthDepleted(RunHealthDepleted evt)
        {
            Fail();
        }

        private void Complete()
        {
            if (!TryEnter(ELevelPhase.Complete))
                return;

            EB.Gameplay.Invoke(new RunFinished(true));

            m_Save.CompleteLevel(m_LevelIndex, m_Score.Score, Mathf.RoundToInt(m_Elapsed));
            m_Feedback.PlayComplete();
            EB.Analytics.Invoke(new MatchWinAnalytics(m_LevelIndex, m_Elapsed));
            EB.Presentation.Invoke(new RunEnded(true, m_Score.Score, m_Score.BestCombo));
        }

        private void Fail()
        {
            if (!TryEnter(ELevelPhase.Fail))
                return;

            EB.Gameplay.Invoke(new RunFinished(false));

            m_Save.TrySpendHeart();
            int attemptCount = m_Save.GetLevelAttempts(m_LevelIndex);
            m_Feedback.PlayFail();
            EB.Analytics.Invoke(new MatchLoseAnalytics(m_LevelIndex, m_Elapsed, attemptCount));
            EB.Presentation.Invoke(new RunEnded(false, m_Score.Score, m_Score.BestCombo));
        }
    }
}
