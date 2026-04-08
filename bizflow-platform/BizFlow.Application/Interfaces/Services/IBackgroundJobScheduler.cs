namespace BizFlow.Application.Interfaces.Services
{
    public interface IBackgroundJobScheduler
    {
        void EnqueueIncrementUsageSql(Guid subscriptionId, string featureCode);
        void EnqueueAiAnomalyCheck(int locationId, string recordType, long recordId);
    }
}

