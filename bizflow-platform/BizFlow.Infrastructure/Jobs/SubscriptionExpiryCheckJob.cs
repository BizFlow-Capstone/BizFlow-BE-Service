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
        private readonly ISubscriptionService _subscriptionService;
        private readonly IFirestoreService _firestoreService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SubscriptionExpiryCheckJob> _logger;

        public SubscriptionExpiryCheckJob(
            IUnitOfWork unitOfWork,
            ISubscriptionService subscriptionService,
            IFirestoreService firestoreService,
            INotificationService notificationService,
            ILogger<SubscriptionExpiryCheckJob> logger)
        {
            _unitOfWork = unitOfWork;
            _subscriptionService = subscriptionService;
            _firestoreService = firestoreService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var cutoffUtc = DateTime.UtcNow.AddMinutes(-30);
            var expiringSubscriptions = await _unitOfWork.Subscriptions.GetSubscriptionsToExpireAsync(cutoffUtc);
            var expiredPaidOwnerIds = new HashSet<Guid>();

            if (expiringSubscriptions.Count == 0)
            {
                return;
            }

            foreach (var subscription in expiringSubscriptions)
            {
                if (IsFreePlan(subscription.SubscriptionPlan))
                {
                    await _subscriptionService.RenewFreeSubscriptionCycleAsync(subscription.SubscriptionId);
                    _logger.LogInformation(
                        "Free subscription cycle renewed for owner {OwnerId}, subscription {SubscriptionId}",
                        subscription.OwnerProfileId,
                        subscription.SubscriptionId);
                    continue;
                }

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
                expiredPaidOwnerIds.Add(subscription.OwnerProfileId);
            }

            await _unitOfWork.SaveChangesAsync();

            foreach (var ownerProfileId in expiredPaidOwnerIds)
            {
                await _subscriptionService.EnsureFreeSubscriptionAsync(ownerProfileId);
            }

            _logger.LogInformation("SubscriptionExpiryCheckJob processed {Count} expiring subscriptions", expiringSubscriptions.Count);
        }

        private static bool IsFreePlan(SubscriptionPlan plan)
        {
            var activePrice = plan.Prices?.FirstOrDefault(p => p.IsActive);
            return activePrice != null && activePrice.GetEffectivePrice() <= 0m;
        }
    }
}
