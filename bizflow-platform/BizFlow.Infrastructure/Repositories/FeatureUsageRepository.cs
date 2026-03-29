using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class FeatureUsageRepository : IFeatureUsageRepository
    {
        private readonly BizFlowDbContext _context;

        public FeatureUsageRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public Task AddRangeAsync(IEnumerable<FeatureUsage> featureUsages)
        {
            return _context.FeatureUsages.AddRangeAsync(featureUsages);
        }

        public void Remove(FeatureUsage usage)
        {
            _context.FeatureUsages.Remove(usage);
        }

        public Task<FeatureUsage?> GetBySubscriptionAndFeatureCodeAsync(Guid subscriptionId, string featureCode)
        {
            return _context.FeatureUsages
                .Include(f => f.Feature)
                .FirstOrDefaultAsync(f => f.SubscriptionId == subscriptionId && f.Feature.FeatureCode == featureCode);
        }

        public Task<List<FeatureUsage>> GetBySubscriptionAsync(Guid subscriptionId)
        {
            return _context.FeatureUsages
                .Include(f => f.Feature)
                .Where(f => f.SubscriptionId == subscriptionId)
                .ToListAsync();
        }
    }
}
