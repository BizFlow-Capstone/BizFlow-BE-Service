using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ISubscriptionPlanPriceRepository
    {
        Task<SubscriptionPlanPrice?> GetActiveByPlanIdAsync(int subscriptionPlanId);
        Task AddAsync(SubscriptionPlanPrice price);
        Task DeactivateByPlanIdAsync(int subscriptionPlanId);
    }
}
