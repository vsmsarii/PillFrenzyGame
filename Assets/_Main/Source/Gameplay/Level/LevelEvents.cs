namespace PillFrenzy.Gameplay
{
    public readonly struct RunStarted
    {
        public readonly LevelDefinitionSO Definition;

        public RunStarted(LevelDefinitionSO definition)
        {
            Definition = definition;
        }
    }

    public readonly struct RunFinished
    {
        public readonly bool IsComplete;

        public RunFinished(bool isComplete)
        {
            IsComplete = isComplete;
        }
    }

    public readonly struct RunHealthDepleted
    {
    }
}
