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
    }
}

