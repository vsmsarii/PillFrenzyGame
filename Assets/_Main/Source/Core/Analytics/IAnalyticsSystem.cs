namespace PillFrenzy.Core
{
    public interface IAnalyticsSystem : IService
    {
        void Register(IAnalytics analytics);
    }
}
