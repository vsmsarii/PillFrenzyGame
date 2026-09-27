namespace PillFrenzy.Gameplay
{
    public readonly struct CapsuleSpawnData
    {
        public readonly CapsuleDefinitionSO Definition;
        public readonly CapsuleColorSO Color;

        public CapsuleSpawnData(CapsuleDefinitionSO definition, CapsuleColorSO color)
        {
            Definition = definition;
            Color = color;
        }
    }
}
