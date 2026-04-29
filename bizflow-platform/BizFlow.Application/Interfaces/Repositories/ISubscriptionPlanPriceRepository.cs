using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ISubscriptionPlanPriceRepository
    {
        Task<SubscriptionPlanPrice?> GetActiveByPlanIdAsync(int subscriptionPlanId);
        Task<List<SubscriptionPlanPrice>> GetByPlanIdsAsync(
            IEnumerable<int> subscriptionPlanIds,
            CancellationToken cancellationToken = default);
        Task AddAsync(SubscriptionPlanPrice price);
        Task DeactivateByPlanIdAsync(int subscriptionPlanId);
    }
}
