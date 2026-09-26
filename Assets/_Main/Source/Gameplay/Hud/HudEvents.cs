namespace PillFrenzy.Gameplay
{
    public readonly struct RunHudChanged
    {
        public readonly int Score;
        public readonly int Combo;
        public readonly int Health;

        public RunHudChanged(int score, int combo, int health)
        {
            Score = score;
            Combo = combo;
            Health = health;
        }
    }

    public readonly struct RunEnded
    {
        public readonly bool IsComplete;
        public readonly int Score;
        public readonly int BestCombo;

        public RunEnded(bool isComplete, int score, int bestCombo)
        {
            IsComplete = isComplete;
            Score = score;
            BestCombo = bestCombo;
        }
    }

    public readonly struct RunTargetFillChanged
    {
        public readonly TargetFill[] Fills;
        public readonly int Remaining;

        public RunTargetFillChanged(TargetFill[] fills, int remaining)
        {
            Fills = fills;
            Remaining = remaining;
        }
    }

    public readonly struct TargetFill
    {
        public readonly CapsuleColorSO Color;
        public readonly int Occupied;
        public readonly int Capacity;

        public TargetFill(CapsuleColorSO color, int occupied, int capacity)
        {
            Color = color;
            Occupied = occupied;
            Capacity = capacity;
        }
    }
}
