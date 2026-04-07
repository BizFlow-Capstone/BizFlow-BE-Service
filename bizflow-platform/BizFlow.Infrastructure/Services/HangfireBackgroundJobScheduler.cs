using BizFlow.Application.Interfaces.Services;
using BizFlow.Infrastructure.Jobs;
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
            // Uses AiAnomalyCheckJob wrapper which calls AI Service + sends FCM on CRITICAL alerts
            BackgroundJob.Enqueue<AiAnomalyCheckJob>(job =>
                job.ExecuteAsync(locationId, recordType, recordId));
        }
    }
}

