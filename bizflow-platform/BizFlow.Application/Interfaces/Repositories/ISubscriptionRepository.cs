using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetActiveByOwnerAsync(Guid ownerProfileId);
        Task<List<Subscription>> GetActiveByPlanIdWithUsagesAsync(int subscriptionPlanId);
        Task<Subscription?> GetByIdWithUsagesAsync(Guid subscriptionId);
        Task<List<Subscription>> GetSubscriptionsToExpireAsync(DateTime cutoffUtc);
        Task<List<Subscription>> GetAllActiveForSyncAsync();
        Task<bool> HasAnySubscriptionAsync(Guid ownerProfileId);
        Task AddAsync(Subscription subscription);
    }
}
