using BizFlow.Application.DTOs.Subscription;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ISubscriptionPlanRepository
    {
        Task<List<SubscriptionPlan>> GetActivePlansWithFeaturesAsync();
        Task<SubscriptionPlan?> GetByIdWithFeaturesAsync(int subscriptionPlanId);
        Task<SubscriptionPlan?> GetByIdAsync(int subscriptionPlanId);
        Task<SubscriptionPlan?> GetByIdWithFeaturesAndPriceAsync(int subscriptionPlanId);
        Task<(IEnumerable<SubscriptionPlan> Items, int TotalCount)> SearchAsync(SubscriptionPlanQueryParams query);
        Task<bool> HasActiveSubscriptionsAsync(int subscriptionPlanId);
        Task AddAsync(SubscriptionPlan plan);
        void Delete(SubscriptionPlan plan);
    }
}
