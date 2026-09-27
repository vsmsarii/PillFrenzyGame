namespace PillFrenzy.Core
{
    public readonly struct AdStarted
    {
        public readonly EAdType Type;

        public AdStarted(EAdType type)
        {
            Type = type;
        }
    }

    public readonly struct AdFinished
    {
        public readonly EAdType Type;
        public readonly EAdResult Result;

        public AdFinished(EAdType type, EAdResult result)
        {
            Type = type;
            Result = result;
        }
    }
}
