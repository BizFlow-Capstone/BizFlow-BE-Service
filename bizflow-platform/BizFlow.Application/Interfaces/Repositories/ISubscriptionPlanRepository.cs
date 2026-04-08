using BizFlow.Application.DTOs.Subscription;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ISubscriptionPlanRepository
    {
        Task<List<SubscriptionPlan>> GetActivePlansWithFeaturesAsync();
        /// <summary>Free plan: active, matches <paramref name="planName"/>, effective price 0 VND.</summary>
        Task<SubscriptionPlan?> GetActiveFreePlanAsync(string planName);
        Task<SubscriptionPlan?> GetByIdWithFeaturesAsync(int subscriptionPlanId);
        Task<SubscriptionPlan?> GetByIdAsync(int subscriptionPlanId);
        Task<SubscriptionPlan?> GetByIdWithFeaturesAndPriceAsync(int subscriptionPlanId);
        Task<(IEnumerable<SubscriptionPlan> Items, int TotalCount)> SearchAsync(SubscriptionPlanQueryParams query);
        Task<bool> HasActiveSubscriptionsAsync(int subscriptionPlanId);
        Task AddAsync(SubscriptionPlan plan);
        void Delete(SubscriptionPlan plan);
    }
}
