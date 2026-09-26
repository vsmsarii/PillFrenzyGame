namespace PillFrenzy.Gameplay
{
    public interface ILevelRunState
    {
        ELevelPhase Phase { get; }
        float Elapsed { get; }
        int LevelIndex { get; }
    }
}
