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

        public RunTargetFillChanged(TargetFill[] fills)
        {
            Fills = fills;
        }
    }

    public readonly struct TargetFill
    {
        public readonly ECapsuleColor Color;
        public readonly int Occupied;
        public readonly int Capacity;

        public TargetFill(ECapsuleColor color, int occupied, int capacity)
        {
            Color = color;
            Occupied = occupied;
            Capacity = capacity;
        }
    }
}
