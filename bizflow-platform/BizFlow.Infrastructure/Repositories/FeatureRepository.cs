using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class FeatureRepository : IFeatureRepository
    {
        private readonly BizFlowDbContext _context;

        public FeatureRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public Task<List<Feature>> GetAllAsync()
        {
            return _context.Features
                .OrderBy(f => f.FeatureId)
                .ToListAsync();
        }

        public Task<Feature?> GetByCodeAsync(string featureCode)
        {
            var code = featureCode.Trim();
            return _context.Features
                .FirstOrDefaultAsync(f => f.FeatureCode.ToUpper() == code.ToUpper());
        }

        public async Task AddAsync(Feature feature)
        {
            await _context.Features.AddAsync(feature);
        }

        public async Task<HashSet<int>> GetExistingIdsAsync(IEnumerable<int> featureIds)
        {
            var distinct = featureIds.Distinct().ToList();
            if (distinct.Count == 0)
                return new HashSet<int>();

            var found = await _context.Features
                .AsNoTracking()
                .Where(f => distinct.Contains(f.FeatureId))
                .Select(f => f.FeatureId)
                .ToListAsync();

            return found.ToHashSet();
        }
    }
}
