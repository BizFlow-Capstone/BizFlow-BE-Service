using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using BizFlow.Infrastructure.DataContext;
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

            var q = _db.GeneralLedgerEntries
                .Where(e => e.BusinessLocationId == query.BusinessLocationId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.TransactionType))
            {
                var transactionType = query.TransactionType.Trim().ToLower();
                q = q.Where(e => e.TransactionType.ToLower() == transactionType);
            }

            if (!string.IsNullOrWhiteSpace(query.ReferenceType))
            {
                var referenceType = query.ReferenceType.Trim().ToLower();
                q = q.Where(e => e.ReferenceType.ToLower() == referenceType);
            }

            if (!string.IsNullOrWhiteSpace(query.MoneyChannel))
            {
                var moneyChannel = query.MoneyChannel.Trim().ToLower();
                q = q.Where(e => e.MoneyChannel != null && e.MoneyChannel.ToLower() == moneyChannel);
            }

            if (query.FromDate.HasValue)
                q = q.Where(e => e.EntryDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                q = q.Where(e => e.EntryDate <= query.ToDate.Value);

            if (viewMode == GeneralLedgerViewMode.Effective)
            {
                if (!query.ToDate.HasValue)
                    throw new ArgumentException("ToDate is required for effective view mode.", nameof(query.ToDate));

                var asOfDate = query.ToDate.Value;

                q = q.Where(e => !e.IsReversal);

                q = q.Where(e => !_db.GeneralLedgerEntries.Any(r =>
                    r.IsReversal
                    && r.ReversedEntryId == e.EntryId
                    && r.EntryDate <= asOfDate));
            }

            var totalCount = await q.CountAsync();
            if (totalCount == 0)
                return (Array.Empty<GeneralLedgerEntry>(), 0);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;

            var items = await q
                .OrderByDescending(e => e.EntryDate)
                .ThenByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.EntryId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
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
    }
}
