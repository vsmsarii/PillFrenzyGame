namespace PillFrenzy.Gameplay
{
    public interface ILevelRunState
    {
        ELevelPhase Phase { get; }
        bool IsPaused { get; }
        bool IsSimulating { get; }
        bool HasEnded { get; }
        float Elapsed { get; }
        int LevelIndex { get; }
    }
}
