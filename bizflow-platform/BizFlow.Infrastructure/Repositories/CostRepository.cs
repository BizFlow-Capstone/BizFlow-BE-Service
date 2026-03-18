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
            var countSpec = new CostSearchSpec(query, isCount: true);
            var countQuery = SpecificationEvaluator<Cost>.GetQuery(_db.Costs.AsQueryable(), countSpec);
            var totalCount = await countQuery.CountAsync();

            if (totalCount == 0)
                return (Array.Empty<Cost>(), 0);

            var spec = new CostSearchSpec(query, isCount: false);
            var queryResult = SpecificationEvaluator<Cost>.GetQuery(_db.Costs.AsQueryable(), spec);
            var items = await queryResult.ToListAsync();

            return (items, totalCount);
        }

        public Task<Cost?> GetByIdAsync(long costId)
            => _db.Costs.FirstOrDefaultAsync(c => c.CostId == costId);

        public Task<Cost?> GetByImportIdAsync(long importId)
            => _db.Costs.FirstOrDefaultAsync(c => c.ImportId == importId);

        public async Task<Cost> AddAsync(Cost cost)
        {
            _db.Costs.Add(cost);
            return cost;
        }

        public void Update(Cost cost)
            => _db.Costs.Update(cost);
    }
}
