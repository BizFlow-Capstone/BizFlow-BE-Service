using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    public class FirestoreSyncJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirestoreService _firestoreService;
        private readonly ILogger<FirestoreSyncJob> _logger;

        public FirestoreSyncJob(
            IUnitOfWork unitOfWork,
            IFirestoreService firestoreService,
            ILogger<FirestoreSyncJob> logger)
        {
            _unitOfWork = unitOfWork;
            _firestoreService = firestoreService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var activeSubscriptions = await _unitOfWork.Subscriptions.GetAllActiveForSyncAsync();

            foreach (var subscription in activeSubscriptions)
            {
                var docId = $"{subscription.OwnerProfileId}_active";
                var usageDoc = await _firestoreService.GetUsageDocAsync(docId);

                if (usageDoc == null)
                {
                    await _firestoreService.SetUsageTrackingAsync(
                        subscription.OwnerProfileId,
                        subscription,
                        subscription.SubscriptionPlan.PlanFeatures.ToList(),
                        subscription.FeatureUsages.ToDictionary(x => x.Feature.FeatureCode, x => x.UsedCount));

                    await _unitOfWork.SubscriptionAuditLogs.AddAsync(new SubscriptionAuditLog
                    {
                        SubscriptionId = subscription.SubscriptionId,
                        Action = SubscriptionAuditAction.FirestoreResync,
                        Details = "{\"reason\":\"usage doc missing\"}",
                        CreatedAt = DateTime.UtcNow
                    });

                    continue;
                }

                foreach (var usage in subscription.FeatureUsages)
                {
                    if (!usageDoc.Features.TryGetValue(usage.Feature.FeatureCode, out var fsFeature))
                    {
                        await _firestoreService.SetFeatureUsedCountAsync(docId, usage.Feature.FeatureCode, usage.UsedCount);
                        continue;
                    }

                    if (fsFeature.Used > usage.UsedCount)
                    {
                        usage.UsedCount = fsFeature.Used;
                        usage.UpdatedAt = DateTime.UtcNow;

                        await _unitOfWork.SubscriptionAuditLogs.AddAsync(new SubscriptionAuditLog
                        {
                            SubscriptionId = subscription.SubscriptionId,
                            Action = SubscriptionAuditAction.UsageSnapshot,
                            Details = $"{{\"feature\":\"{usage.Feature.FeatureCode}\",\"source\":\"firestore\"}}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    else if (usage.UsedCount > fsFeature.Used)
                    {
                        await _firestoreService.SetFeatureUsedCountAsync(docId, usage.Feature.FeatureCode, usage.UsedCount);

                        await _unitOfWork.SubscriptionAuditLogs.AddAsync(new SubscriptionAuditLog
                        {
                            SubscriptionId = subscription.SubscriptionId,
                            Action = SubscriptionAuditAction.FirestoreAnomaly,
                            Details = $"{{\"feature\":\"{usage.Feature.FeatureCode}\",\"sql\":{usage.UsedCount},\"firestore\":{fsFeature.Used}}}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("FirestoreSyncJob synced {Count} active subscriptions", activeSubscriptions.Count);
        }
    }
}
