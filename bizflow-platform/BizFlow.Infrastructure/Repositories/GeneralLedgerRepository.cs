using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using BizFlow.Infrastructure.DataContext;
using BizFlow.Application.Specifications.GeneralLedger;
using BizFlow.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class GeneralLedgerRepository : IGeneralLedgerRepository
    {
        private readonly BizFlowDbContext _db;

        public GeneralLedgerRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<GeneralLedgerEntry> Items, int TotalCount)> SearchAsync(GeneralLedgerQueryParams query)
        {
            var viewMode = (query.ViewMode ?? GeneralLedgerViewMode.Audit).Trim().ToLowerInvariant();
            if (!GeneralLedgerViewMode.IsValid(viewMode))
                throw new ArgumentException("Invalid general ledger view mode.", nameof(query.ViewMode));

            query.ViewMode = viewMode;

            // 1. Get Total Count
            var countSpec = new GeneralLedgerSearchSpec(query, isCount: true);
            var countQuery = SpecificationEvaluator<GeneralLedgerEntry>.GetQuery(_db.GeneralLedgerEntries.AsQueryable(), countSpec);
            var totalCount = await countQuery.CountAsync();

            if (totalCount == 0)
                return (Array.Empty<GeneralLedgerEntry>(), 0);

            // 2. Deferred Join Strategy (Get IDs first)
            var filterSpec = new GeneralLedgerSearchSpec(query, isCount: false, filterOnly: true);
            var filterQuery = SpecificationEvaluator<GeneralLedgerEntry>.GetQuery(_db.GeneralLedgerEntries.AsQueryable(), filterSpec);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;

            var ids = await filterQuery
                .OrderByDescending(e => e.EntryDate)
                .ThenByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.EntryId)
                .Select(e => e.EntryId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!ids.Any())
                return (Array.Empty<GeneralLedgerEntry>(), totalCount);

            // 3. Fetch full entities
            var items = await _db.GeneralLedgerEntries
                .Where(e => ids.Contains(e.EntryId))
                .OrderByDescending(e => e.EntryDate)
                .ThenByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.EntryId)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<GeneralLedgerEntry> AddAsync(GeneralLedgerEntry entry)
        {
            _db.GeneralLedgerEntries.Add(entry);
            return entry;
        }

        public async Task<IEnumerable<GeneralLedgerEntry>> GetNonReversalByReferenceAsync(int businessLocationId, string referenceType, long referenceId)
        {
            return await _db.GeneralLedgerEntries
                .Where(e => e.BusinessLocationId == businessLocationId
                    && e.ReferenceType == referenceType
                    && e.ReferenceId == referenceId
                    && !e.IsReversal)
                .OrderBy(e => e.EntryId)
                .ToListAsync();
        }

        public async Task<HashSet<long>> GetReversedEntryIdsAsync(IEnumerable<long> entryIds)
        {
            var ids = entryIds.Distinct().ToList();
            if (!ids.Any())
                return new HashSet<long>();

            var reversedIds = await _db.GeneralLedgerEntries
                .Where(e => e.IsReversal && e.ReversedEntryId.HasValue && ids.Contains(e.ReversedEntryId.Value))
                .Select(e => e.ReversedEntryId!.Value)
                .ToListAsync();

            return new HashSet<long>(reversedIds);
        }

        public async Task<List<(long EntryId, long? ReversedEntryId)>> GetEntryLinksByIdsAsync(IEnumerable<long> entryIds)
        {
            var ids = entryIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (!ids.Any())
                return [];

            return await _db.GeneralLedgerEntries
                .Where(e => ids.Contains(e.EntryId))
                .Select(e => new ValueTuple<long, long?>(e.EntryId, e.ReversedEntryId))
                .ToListAsync();
        }

        public async Task<Dictionary<long, long?>> GetReferenceIdsByEntryIdsAsync(IEnumerable<long> entryIds)
        {
            var ids = entryIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (!ids.Any())
                return [];

            return await _db.GeneralLedgerEntries
                .Where(e => ids.Contains(e.EntryId))
                .ToDictionaryAsync(e => e.EntryId, e => e.ReferenceId);
        }

        public async Task<Dictionary<long, (int ReversalCount, long? LatestReversalEntryId)>> GetReversalSummaryAsOfAsync(
            IEnumerable<long> entryIds,
            DateOnly asOfDate)
        {
            var ids = entryIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (!ids.Any())
                return new Dictionary<long, (int ReversalCount, long? LatestReversalEntryId)>();

            var grouped = await _db.GeneralLedgerEntries
                .Where(e => e.IsReversal
                    && e.ReversedEntryId.HasValue
                    && ids.Contains(e.ReversedEntryId.Value)
                    && e.EntryDate <= asOfDate)
                .GroupBy(e => e.ReversedEntryId!.Value)
                .Select(g => new
                {
                    OriginalEntryId = g.Key,
                    ReversalCount = g.Count(),
                    LatestReversalEntryId = g.Max(x => x.EntryId)
                })
                .ToListAsync();

            return grouped.ToDictionary(
                x => x.OriginalEntryId,
                x => (x.ReversalCount, (long?)x.LatestReversalEntryId));
        }

        public async Task<decimal> AggregateByLocationAndPeriodAsync(
            int locationId, DateOnly from, DateOnly to,
            string aggType, string field,
            string? moneyChannel, string? transactionType)
        {
            var query = _db.GeneralLedgerEntries
                .Where(e => e.BusinessLocationId == locationId
                    && !e.IsReversal
                    && e.EntryDate >= from
                    && e.EntryDate <= to);

            if (!string.IsNullOrEmpty(moneyChannel))
                query = query.Where(e => e.MoneyChannel == moneyChannel);

            if (!string.IsNullOrEmpty(transactionType))
                query = query.Where(e => e.TransactionType == transactionType);

            return (aggType.ToUpper(), field) switch
            {
                ("SUM", "DebitAmount") => await query.SumAsync(e => e.DebitAmount),
                ("SUM", "CreditAmount") => await query.SumAsync(e => e.CreditAmount),
                ("AVG", "DebitAmount") => await query.AnyAsync() ? await query.AverageAsync(e => e.DebitAmount) : 0m,
                ("AVG", "CreditAmount") => await query.AnyAsync() ? await query.AverageAsync(e => e.CreditAmount) : 0m,
                ("COUNT", _) => await query.CountAsync(),
                _ => 0m
            };
        }

        public async Task<(decimal TotalRevenue, decimal TotalCost)> SumRevenueAndCostAsync(GeneralLedgerTotalsQueryParams query)
        {
            var baseQuery = _db.GeneralLedgerEntries
                .Where(e => e.BusinessLocationId == query.BusinessLocationId
                    && e.EntryDate >= query.FromDate!.Value
                    && e.EntryDate <= query.ToDate!.Value);

            var totalRevenue = await baseQuery
                .Where(e => e.ReferenceType == GeneralLedgerReferenceType.Revenue)
                .SumAsync(e => e.DebitAmount - e.CreditAmount);

            var totalCost = await baseQuery
                .Where(e => e.ReferenceType == GeneralLedgerReferenceType.Cost)
                .SumAsync(e => e.CreditAmount - e.DebitAmount);

            return (totalRevenue, totalCost);
        }
    }
}
