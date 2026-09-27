namespace PillFrenzy.Gameplay
{
    public sealed class PreRunContext
    {
        public int LevelIndex { get; }
        public LevelDefinitionSO Definition { get; }

        public PreRunContext(int levelIndex, LevelDefinitionSO definition)
        {
            LevelIndex = levelIndex;
            Definition = definition;
        }
    }
}
