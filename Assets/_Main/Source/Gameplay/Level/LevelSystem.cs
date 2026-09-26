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
        private float m_Elapsed;

        public ELevelPhase Phase => m_Phase;
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

        public void StartRun(LevelDefinitionSO definition)
        {
            if (definition == null)
                return;

            m_Phase = ELevelPhase.Playing;
            m_Elapsed = 0f;

            m_Save.IncrementLevelAttempts(m_LevelIndex);

            EB.Gameplay.Invoke(new RunStarted(definition));
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

        public void Tick(float deltaTime)
        {
            if (m_Phase != ELevelPhase.Playing)
                return;

            m_Elapsed += deltaTime;
        }

        public void Shutdown()
        {
            EB.Gameplay.Remove<AllTargetsFilled>(OnAllTargetsFilled);
            EB.Gameplay.Remove<RunHealthDepleted>(OnRunHealthDepleted);
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
            if (m_Phase != ELevelPhase.Playing)
                return;

            m_Phase = ELevelPhase.Complete;
            EB.Gameplay.Invoke(new RunFinished(true));

            m_Save.CompleteLevel(m_LevelIndex, m_Score.Score, Mathf.RoundToInt(m_Elapsed));
            m_Feedback.PlayComplete();
            EB.Analytics.Invoke(new MatchWinAnalytics(m_LevelIndex, m_Elapsed));
            EB.Presentation.Invoke(new RunEnded(true, m_Score.Score, m_Score.BestCombo));
        }

        private void Fail()
        {
            if (m_Phase != ELevelPhase.Playing)
                return;

            m_Phase = ELevelPhase.Fail;
            EB.Gameplay.Invoke(new RunFinished(false));

            m_Save.TrySpendHeart();
            int attemptCount = m_Save.GetLevelAttempts(m_LevelIndex);
            m_Feedback.PlayFail();
            EB.Analytics.Invoke(new MatchLoseAnalytics(m_LevelIndex, m_Elapsed, attemptCount));
            EB.Presentation.Invoke(new RunEnded(false, m_Score.Score, m_Score.BestCombo));
        }
    }
}
