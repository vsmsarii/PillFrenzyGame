namespace PillFrenzy.Gameplay
{
    public readonly struct CapsuleSpawnData
    {
        public readonly CapsuleDefinitionSO Definition;
        public readonly CapsuleColorSO Color;
        public readonly float Speed;

        public CapsuleSpawnData(CapsuleDefinitionSO definition, CapsuleColorSO color, float speed)
        {
            Definition = definition;
            Color = color;
            Speed = speed;
        }
    }
}
