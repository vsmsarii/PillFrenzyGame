namespace PillFrenzy.Gameplay
{
    public readonly struct CapsuleResolved
    {
        public readonly ECapsuleKind Kind;

        public CapsuleResolved(ECapsuleKind kind)
        {
            Kind = kind;
        }
    }

    public readonly struct AllTargetsFilled
    {
    }
}
