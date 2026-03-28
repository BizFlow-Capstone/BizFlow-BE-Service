namespace BizFlow.Application.Interfaces.Services
{
    public interface IBackgroundJobScheduler
    {
        void EnqueueIncrementUsageSql(Guid subscriptionId, string featureCode);
    }
}

