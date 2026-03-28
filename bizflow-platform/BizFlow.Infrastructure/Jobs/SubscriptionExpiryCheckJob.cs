using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class SubscriptionExpiryCheckJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirestoreService _firestoreService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SubscriptionExpiryCheckJob> _logger;

        public SubscriptionExpiryCheckJob(
            IUnitOfWork unitOfWork,
            IFirestoreService firestoreService,
            INotificationService notificationService,
            ILogger<SubscriptionExpiryCheckJob> logger)
        {
            _unitOfWork = unitOfWork;
            _firestoreService = firestoreService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var cutoffUtc = DateTime.UtcNow.AddMinutes(-30);
            var expiringSubscriptions = await _unitOfWork.Subscriptions.GetSubscriptionsToExpireAsync(cutoffUtc);

            if (expiringSubscriptions.Count == 0)
            {
                return;
            }

            foreach (var subscription in expiringSubscriptions)
            {
                subscription.Status = SubscriptionStatus.Expired;
                subscription.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SubscriptionAuditLogs.AddAsync(new SubscriptionAuditLog
                {
                    SubscriptionId = subscription.SubscriptionId,
                    Action = SubscriptionAuditAction.Expired,
                    Details = "{\"reason\":\"Expired by scheduler\"}",
                    CreatedAt = DateTime.UtcNow
                });

                await _firestoreService.MarkUsageTrackingExpiredAsync(subscription.OwnerProfileId);
                await _notificationService.NotifySubscriptionExpiredAsync(subscription.OwnerProfileId, subscription.SubscriptionPlan.Name);
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("SubscriptionExpiryCheckJob expired {Count} subscriptions", expiringSubscriptions.Count);
        }
    }
}
