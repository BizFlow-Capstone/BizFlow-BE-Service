using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class UsageSnapshotJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UsageSnapshotJob> _logger;

        public UsageSnapshotJob(IUnitOfWork unitOfWork, ILogger<UsageSnapshotJob> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var activeSubscriptions = await _unitOfWork.Subscriptions.GetAllActiveForSyncAsync();
            var now = DateTime.UtcNow;

            foreach (var subscription in activeSubscriptions)
            {
                await _unitOfWork.SubscriptionAuditLogs.AddAsync(new SubscriptionAuditLog
                {
                    SubscriptionId = subscription.SubscriptionId,
                    Action = SubscriptionAuditAction.UsageSnapshot,
                    Details = $"{{\"snapshotAt\":\"{now:O}\",\"usageCount\":{subscription.FeatureUsages.Count}}}",
                    CreatedAt = now
                });
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("UsageSnapshotJob completed for {Count} subscriptions", activeSubscriptions.Count);
        }
    }
}
