using BizFlow.Application.DTOs.Cost;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Specifications.Costs;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using BizFlow.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class CostRepository : ICostRepository
    {
        private readonly BizFlowDbContext _db;

        public CostRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Cost> Items, int TotalCount)> SearchAsync(CostQueryParams query)
        {
            // 1. Get Total Count
            var countSpec = new CostSearchSpec(query, isCount: true);
            var countQuery = SpecificationEvaluator<Cost>.GetQuery(_db.Costs.AsQueryable(), countSpec);
            var totalCount = await countQuery.CountAsync();

            if (totalCount == 0)
                return (Array.Empty<Cost>(), 0);

            // 2. Deferred Join Strategy (Get IDs first)
            var filterSpec = new CostSearchSpec(query, isCount: false, filterOnly: true);
            var filterQuery = SpecificationEvaluator<Cost>.GetQuery(_db.Costs.AsQueryable(), filterSpec);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;

            var ids = await filterQuery
                .Select(c => c.CostId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!ids.Any())
                return (Array.Empty<Cost>(), totalCount);

            // 3. Fetch full entities
            var items = await _db.Costs
                .Where(c => ids.Contains(c.CostId))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task<Cost?> GetByIdAsync(long costId)
            => _db.Costs.FirstOrDefaultAsync(c => c.CostId == costId);

        public Task<Cost?> GetByImportIdAsync(long importId)
            => _db.Costs.FirstOrDefaultAsync(c => c.ImportId == importId);

        public async Task<IEnumerable<Cost>> GetByIdsAsync(IEnumerable<long> costIds)
        {
            if (costIds == null || !costIds.Any()) return Array.Empty<Cost>();
            return await _db.Costs.Where(c => costIds.Contains(c.CostId)).ToListAsync();
        }

        public async Task<Cost> AddAsync(Cost cost)
        {
            _db.Costs.Add(cost);
            return cost;
        }

        public void Update(Cost cost)
            => _db.Costs.Update(cost);
    }
}
