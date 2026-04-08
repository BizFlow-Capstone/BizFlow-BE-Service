using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Specifications.SubscriptionPlans;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using BizFlow.Infrastructure.DataContext;
using BizFlow.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly BizFlowDbContext _context;

        public SubscriptionPlanRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public Task<List<SubscriptionPlan>> GetActivePlansWithFeaturesAsync()
        {
            return _context.SubscriptionPlans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .Include(p => p.Prices)
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public Task<SubscriptionPlan?> GetActiveFreePlanAsync(string planName)
        {
            return _context.SubscriptionPlans
                .AsNoTracking()
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .Include(p => p.Prices)
                .Where(p => p.IsActive == true
                    && p.DeletedAt == null
                    && p.Name == planName
                    && p.Prices.Any(pr => pr.IsActive && pr.BasePrice == 0))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public Task<SubscriptionPlan?> GetByIdWithFeaturesAsync(int subscriptionPlanId)
        {
            return _context.SubscriptionPlans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .FirstOrDefaultAsync(p => p.SubscriptionPlanId == subscriptionPlanId && p.IsActive == true);
        }

        public Task<SubscriptionPlan?> GetByIdAsync(int subscriptionPlanId)
        {
            return _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.SubscriptionPlanId == subscriptionPlanId);
        }

        public Task<SubscriptionPlan?> GetByIdWithFeaturesAndPriceAsync(int subscriptionPlanId)
        {
            return _context.SubscriptionPlans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .Include(p => p.Prices)
                .FirstOrDefaultAsync(p => p.SubscriptionPlanId == subscriptionPlanId);
        }

        /// <summary>
        /// Deferred Join search — follows ProductRepository.SearchAsync pattern
        /// </summary>
        public async Task<(IEnumerable<SubscriptionPlan> Items, int TotalCount)> SearchAsync(SubscriptionPlanQueryParams query)
        {
            // 1. Count
            var countSpec = new SubscriptionPlanSearchSpec(query, isCount: true);
            var countQuery = SpecificationEvaluator<SubscriptionPlan>.GetQuery(_context.SubscriptionPlans.AsQueryable(), countSpec);
            var totalCount = await countQuery.CountAsync();

            if (totalCount == 0)
                return (new List<SubscriptionPlan>(), 0);

            // 2. Get IDs with paging (filter only)
            var filterSpec = new SubscriptionPlanSearchSpec(query, isCount: false, filterOnly: true);
            var filterQuery = SpecificationEvaluator<SubscriptionPlan>.GetQuery(_context.SubscriptionPlans.AsQueryable(), filterSpec);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;

            var ids = await filterQuery
                .Select(p => p.SubscriptionPlanId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!ids.Any())
                return (new List<SubscriptionPlan>(), totalCount);

            // 3. Fetch full entities by IDs
            var items = await _context.SubscriptionPlans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .Include(p => p.Prices)
                .Where(p => ids.Contains(p.SubscriptionPlanId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task<bool> HasActiveSubscriptionsAsync(int subscriptionPlanId)
        {
            return _context.Subscriptions
                .AnyAsync(s => s.SubscriptionPlanId == subscriptionPlanId
                    && s.Status == SubscriptionStatus.Active);
        }

        public async Task AddAsync(SubscriptionPlan plan)
        {
            await _context.SubscriptionPlans.AddAsync(plan);
        }

        public void Delete(SubscriptionPlan plan)
        {
            _context.SubscriptionPlans.Remove(plan);
        }
    }
}

