using BizFlow.Application.DTOs.Cost;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Specifications.Costs;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
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

        public Task LockCostRowForUpdateAsync(long costId, CancellationToken cancellationToken = default)
            => _db.Database.ExecuteSqlRawAsync(
                "SELECT CostId FROM `Costs` WHERE CostId = {0} LIMIT 1 FOR UPDATE",
                new object[] { costId },
                cancellationToken);

        public Task<bool> ExistsByDocumentNumberAsync(
            int businessLocationId,
            string documentNumberNormalized,
            long? excludeCostId = null,
            CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the uniqueness rule applies to ALL rows — including cancelled or replaced.
            var q = _db.Costs
                .IgnoreQueryFilters()
                .Where(c => c.BusinessLocationId == businessLocationId
                            && c.DocumentNumberNormalized == documentNumberNormalized);

            if (excludeCostId.HasValue)
                q = q.Where(c => c.CostId != excludeCostId.Value);

            return q.AnyAsync(cancellationToken);
        }

        public Task<bool> ExistsByDocumentNumberForOwnerAsync(
            Guid ownerId,
            string documentNumberNormalized,
            long? excludeCostId = null,
            CancellationToken cancellationToken = default)
        {
            // Owner of a location is resolved via UserLocationAssignments (IsOwner = true).
            var ownedLocationIds = _db.UserLocationAssignments
                .Where(ula => ula.UserId == ownerId && ula.IsOwner)
                .Select(ula => ula.BusinessLocationId);

            // IgnoreQueryFilters: uniqueness applies to ALL rows — including cancelled or replaced.
            var q = _db.Costs
                .IgnoreQueryFilters()
                .Where(c => ownedLocationIds.Contains(c.BusinessLocationId)
                            && c.DocumentNumberNormalized == documentNumberNormalized);

            if (excludeCostId.HasValue)
                q = q.Where(c => c.CostId != excludeCostId.Value);

            return q.AnyAsync(cancellationToken);
        }

        public Task<Cost?> GetLatestReplacementByRefCostIdAsync(long refCostId, CancellationToken cancellationToken = default)
            => _db.Costs
                .IgnoreQueryFilters()
                .Where(c => c.RefCostId == refCostId)
                .OrderByDescending(c => c.CostId)
                .FirstOrDefaultAsync(cancellationToken);

        public Task<Cost?> GetLatestReplacementByRefCostIdAsync(
            long refCostId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => _db.Costs
                .IgnoreQueryFilters()
                .Where(c => c.RefCostId == refCostId && c.IdempotencyKey == idempotencyKey)
                .OrderByDescending(c => c.CostId)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<decimal> SumAmountByLocationsAndDateRangeAsync(
            IReadOnlyCollection<int> businessLocationIds,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default)
        {
            if (businessLocationIds == null || businessLocationIds.Count == 0)
                return 0m;

            return await _db.Costs
                .Where(c => businessLocationIds.Contains(c.BusinessLocationId)
                    && c.Status != CostStatus.Cancelled
                    && c.Status != CostStatus.Replaced
                    && c.CostDate >= fromDate
                    && c.CostDate <= toDate)
                .SumAsync(c => c.Amount, cancellationToken);
        }

        public async Task<decimal> AggregateByLocationAndPeriodAsync(
            int locationId, DateOnly from, DateOnly to,
            string aggType)
        {
            var query = _db.Costs
                .Where(c => c.BusinessLocationId == locationId
                    && c.Status != CostStatus.Cancelled
                    && c.Status != CostStatus.Replaced
                    && c.CostDate >= from
                    && c.CostDate <= to);

            return aggType.ToUpper() switch
            {
                "SUM" => await query.SumAsync(c => c.Amount),
                "AVG" => await query.AnyAsync() ? await query.AverageAsync(c => c.Amount) : 0m,
                "COUNT" => await query.CountAsync(),
                _ => 0m
            };
        }

        public async Task<Dictionary<string, decimal>> SumGroupedByBusinessTypeAsync(
            int locationId, DateOnly from, DateOnly to)
        {
            return await _db.Costs
                .Where(c => c.BusinessLocationId == locationId
                    && c.Status != CostStatus.Cancelled
                    && c.Status != CostStatus.Replaced
                    && c.BusinessTypeId.HasValue
                    && c.CostDate >= from
                    && c.CostDate <= to)
                .GroupBy(c => c.BusinessTypeId!.Value)
                .ToDictionaryAsync(
                    g => g.Key.ToString(),
                    g => g.Sum(c => c.Amount));
        }
    }
}
