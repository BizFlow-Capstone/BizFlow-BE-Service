using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IFeatureUsageRepository
    {
        Task AddRangeAsync(IEnumerable<FeatureUsage> featureUsages);
        void Remove(FeatureUsage usage);
        Task<FeatureUsage?> GetBySubscriptionAndFeatureCodeAsync(Guid subscriptionId, string featureCode);
        Task<List<FeatureUsage>> GetBySubscriptionAsync(Guid subscriptionId);
    }
}
