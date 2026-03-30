using BizFlow.Application.Common.Models;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IFirestoreService
    {
        Task SetUsageTrackingAsync(Guid ownerProfileId, Subscription sub, IReadOnlyCollection<PlanFeature> features, IReadOnlyDictionary<string, int>? existingUsed = null);
        Task IncrementFeatureUsageAsync(Guid ownerProfileId, string featureCode);
        Task SetFeatureUsedCountAsync(string docId, string featureCode, int count);
        Task MarkUsageTrackingExpiredAsync(Guid ownerProfileId);
        Task<UsageTrackingDoc?> GetUsageDocAsync(string docId);
        Task<FeatureUsageSnapshot?> GetUsageSnapshotAsync(Guid ownerProfileId, string featureCode);
        Task<FirestoreHealthStatus> GetHealthStatusAsync();
        Task<FirestoreGateDebugStatus> GetFirestoreGateDebugAsync();
        Task<bool> CheckHealthAsync();
        Task UpsertSubscriptionAccessGrantAsync(Guid ownerProfileId, Guid memberProfileId, bool canReadUsage, bool isActive);
        Task RevokeSubscriptionAccessGrantAsync(Guid ownerProfileId, Guid memberProfileId);
    }
}
