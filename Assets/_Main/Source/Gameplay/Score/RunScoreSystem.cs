using PillFrenzy.Core;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class RunScoreSystem
    {
        private readonly ISaveService m_Save;

        private LevelDefinitionSO m_Definition;
        private bool m_Active;
        private int m_Score;
        private int m_Combo;
        private int m_BestCombo;
        private int m_Health;

        public int Score => m_Score;
        public int Combo => m_Combo;
        public int BestCombo => m_BestCombo;
        public int Health => m_Health;

        public RunScoreSystem(ISaveService save)
        {
            m_Save = save;

            EB.Gameplay.Add<RunStarted>(OnRunStarted);
            EB.Gameplay.Add<RunFinished>(OnRunFinished);
            EB.Gameplay.Add<CapsuleResolved>(OnCapsuleResolved);
        }

        public void Shutdown()
        {
            EB.Gameplay.Remove<RunStarted>(OnRunStarted);
            EB.Gameplay.Remove<RunFinished>(OnRunFinished);
            EB.Gameplay.Remove<CapsuleResolved>(OnCapsuleResolved);
            m_Definition = null;
            m_Active = false;
        }

        private void OnRunStarted(RunStarted evt)
        {
            m_Definition = evt.Definition;
            m_Active = true;
            m_Score = 0;
            m_Combo = 0;
            m_BestCombo = 0;
            m_Health = m_Definition.StartingHealth;
            PublishHud();
        }

        private void OnRunFinished(RunFinished evt)
        {
            m_Active = false;
        }

        private void OnCapsuleResolved(CapsuleResolved evt)
        {
            if (!m_Active)
                return;

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

        private void OnCorrect()
        {
            m_Combo++;
            m_BestCombo = Mathf.Max(m_BestCombo, m_Combo);

            m_Score += m_Definition.ScorePerCorrect * m_Combo;
            PublishHud();
        }

        private void OnGold()
        {
            m_Score += m_Definition.ScorePerCorrect * Mathf.Max(1, m_Combo);
            PublishHud();
        }

        private void OnPoison()
        {
            m_Combo = 0;
            if (!m_Save.IsImmortalActive)
                m_Health--;

            PublishHud();
            if (m_Health <= 0)
                EB.Gameplay.Invoke(new RunHealthDepleted());
        }

        private void PublishHud()
        {
            EB.Presentation.Invoke(new RunHudChanged(m_Score, m_Combo, m_Health));
        }
    }
}
