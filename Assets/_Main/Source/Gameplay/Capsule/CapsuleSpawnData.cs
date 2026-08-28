namespace PillFrenzy.Gameplay
{
    public sealed class CapsuleSpawnData
    {
        public readonly CapsuleDefinitionSO Definition;
        public readonly float StartDistance;
        public readonly float Speed;

        public CapsuleSpawnData(CapsuleDefinitionSO definition, float startDistance, float speed)
        {
            Definition = definition;
            StartDistance = startDistance;
            Speed = speed;
        }
    }
}
