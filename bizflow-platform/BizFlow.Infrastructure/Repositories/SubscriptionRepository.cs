using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly BizFlowDbContext _context;

        public SubscriptionRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public Task<Subscription?> GetActiveByOwnerAsync(Guid ownerProfileId)
        {
            return _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(plan => plan.PlanFeatures)
                        .ThenInclude(pf => pf.Feature)
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(plan => plan.Prices)
                .Include(s => s.FeatureUsages)
                    .ThenInclude(u => u.Feature)
                .FirstOrDefaultAsync(s => s.OwnerProfileId == ownerProfileId && s.Status == SubscriptionStatus.Active);
        }

        public Task<List<Subscription>> GetActiveByPlanIdWithUsagesAsync(int subscriptionPlanId)
        {
            return _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Include(s => s.FeatureUsages)
                    .ThenInclude(u => u.Feature)
                .Where(s => s.SubscriptionPlanId == subscriptionPlanId && s.Status == SubscriptionStatus.Active)
                .ToListAsync();
        }

        public Task<Subscription?> GetByIdWithUsagesAsync(Guid subscriptionId)
        {
            return _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Include(s => s.FeatureUsages)
                    .ThenInclude(u => u.Feature)
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
        }

        public Task<List<Subscription>> GetSubscriptionsToExpireAsync(DateTime cutoffUtc)
        {
            return _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(p => p.Prices)
                .Include(s => s.FeatureUsages)
                    .ThenInclude(u => u.Feature)
                .Where(s => s.Status == SubscriptionStatus.Active
                    && s.EndDate < cutoffUtc
                    && (s.SubscriptionPlan.DurationDays > 0
                        || s.SubscriptionPlan.Prices.Any(p => p.IsActive && p.BasePrice == 0)))
                .ToListAsync();
        }

        public Task<List<Subscription>> GetAllActiveForSyncAsync()
        {
            return _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(p => p.PlanFeatures)
                        .ThenInclude(pf => pf.Feature)
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(p => p.Prices)
                .Include(s => s.FeatureUsages)
                    .ThenInclude(u => u.Feature)
                .Where(s => s.Status == SubscriptionStatus.Active)
                .ToListAsync();
        }

        public Task<List<Guid>> GetProfileIdsWithoutActiveSubscriptionAsync()
        {
            return _context.Profiles
                .Where(p => !_context.Subscriptions.Any(s =>
                    s.OwnerProfileId == p.ProfileId
                    && s.Status == SubscriptionStatus.Active))
                .Select(p => p.ProfileId)
                .ToListAsync();
        }

        public Task<bool> HasAnySubscriptionAsync(Guid ownerProfileId)
        {
            return _context.Subscriptions
                .AnyAsync(s => s.OwnerProfileId == ownerProfileId);
        }

        public Task AddAsync(Subscription subscription)
        {
            return _context.Subscriptions.AddAsync(subscription).AsTask();
        }

        public Task AddRangeAsync(IEnumerable<Subscription> subscriptions)
        {
            return _context.Subscriptions.AddRangeAsync(subscriptions);
        }
    }
}
