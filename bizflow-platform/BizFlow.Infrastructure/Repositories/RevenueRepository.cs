using BizFlow.Application.DTOs.Revenue;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

using BizFlow.Application.Specifications.Revenues;
using BizFlow.Infrastructure.Specifications;
using BizFlow.Domain.Enums;

namespace BizFlow.Infrastructure.Repositories
{
    public class RevenueRepository : IRevenueRepository
    {
        private readonly BizFlowDbContext _db;

        public RevenueRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Revenue> Items, int TotalCount)> SearchAsync(RevenueQueryParams query)
        {
            // 1. Get Total Count
            var countSpec = new RevenueSearchSpec(query, isCount: true);
            var countQuery = SpecificationEvaluator<Revenue>.GetQuery(_db.Revenues.AsQueryable(), countSpec);
            var total = await countQuery.CountAsync();

            if (total == 0)
                return (Array.Empty<Revenue>(), 0);

            // 2. Deferred Join Strategy (Get IDs first)
            var filterSpec = new RevenueSearchSpec(query, isCount: false, filterOnly: true);
            var filterQuery = SpecificationEvaluator<Revenue>.GetQuery(_db.Revenues.AsQueryable(), filterSpec);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;

            var ids = await filterQuery
                .Select(r => r.RevenueId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!ids.Any())
                return (Array.Empty<Revenue>(), total);

            // 3. Fetch full entities
            var items = await _db.Revenues
                .Where(r => ids.Contains(r.RevenueId))
                .Include(r => r.BusinessType)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return (items, total);
        }

        public Task<Revenue?> GetByIdAsync(long revenueId)
            => _db.Revenues
                .Include(r => r.BusinessType)
                .FirstOrDefaultAsync(r => r.RevenueId == revenueId);

        public async Task<List<Revenue>> GetSaleByOrderIdAsync(int businessLocationId, long orderId)
        {
            return await _db.Revenues
                .Where(r => r.BusinessLocationId == businessLocationId
                    && r.RevenueType == RevenueType.Sale
                    && r.DeletedAt == null
                    && r.OrderId == orderId)
                .OrderBy(r => r.RevenueId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Revenue>> GetByIdsAsync(IEnumerable<long> revenueIds)
        {
            if (revenueIds == null || !revenueIds.Any()) return Array.Empty<Revenue>();
            return await _db.Revenues
                .Where(r => revenueIds.Contains(r.RevenueId))
                .Include(r => r.BusinessType)
                .ToListAsync();
        }

        public async Task<Revenue> AddAsync(Revenue revenue)
        {
            _db.Revenues.Add(revenue);
            return revenue;
        }

        public void Update(Revenue revenue)
            => _db.Revenues.Update(revenue);

        public async Task<decimal> SumAmountByLocationsAndDateRangeAsync(
            IReadOnlyCollection<int> businessLocationIds,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default)
        {
            if (businessLocationIds == null || businessLocationIds.Count == 0)
                return 0m;

            return await _db.Revenues
                .Where(r => businessLocationIds.Contains(r.BusinessLocationId)
                    && r.RevenueDate >= fromDate
                    && r.RevenueDate <= toDate)
                .SumAsync(r => r.Amount, cancellationToken);
        }

        public async Task<decimal> AggregateByLocationAndPeriodAsync(
            int locationId, DateOnly from, DateOnly to,
            string aggType, string[] revenueTypes)
        {
            var query = _db.Revenues
                .Where(r => r.BusinessLocationId == locationId
                    && r.DeletedAt == null
                    && r.RevenueDate >= from
                    && r.RevenueDate <= to);

            if (revenueTypes.Length > 0)
                query = query.Where(r => revenueTypes.Contains(r.RevenueType));

            return aggType.ToUpper() switch
            {
                "SUM" => await query.SumAsync(r => r.Amount),
                "AVG" => await query.AnyAsync() ? await query.AverageAsync(r => r.Amount) : 0m,
                "COUNT" => await query.CountAsync(),
                _ => 0m
            };
        }

        public async Task<Dictionary<string, decimal>> SumGroupedByBusinessTypeAsync(
            int locationId, DateOnly from, DateOnly to)
        {
            return await _db.Revenues
                .Where(r => r.BusinessLocationId == locationId
                    && r.DeletedAt == null
                    && r.BusinessTypeId.HasValue
                    && r.RevenueDate >= from
                    && r.RevenueDate <= to)
                .GroupBy(r => r.BusinessTypeId!.Value)
                .ToDictionaryAsync(
                    g => g.Key.ToString(),
                    g => g.Sum(r => r.Amount));
        }
    }
}
