using BizFlow.Application.Interfaces.Services;
using Hangfire;

namespace BizFlow.Infrastructure.Services
{
    public class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
    {
        public void EnqueueIncrementUsageSql(Guid subscriptionId, string featureCode)
        {
            BackgroundJob.Enqueue<ISubscriptionService>(service =>
                service.IncrementUsageSqlBackgroundAsync(subscriptionId, featureCode));
        }

        public void EnqueueAiAnomalyCheck(int locationId, string recordType, long recordId)
        {
            BackgroundJob.Enqueue<IAiServiceClient>(client =>
                client.CheckAnomalyAsync(locationId, recordType, recordId, CancellationToken.None));
        }
    }
}

