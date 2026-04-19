using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IGeneralLedgerRepository
    {
        Task<(IEnumerable<GeneralLedgerEntry> Items, int TotalCount)> SearchAsync(GeneralLedgerQueryParams query);
        Task<GeneralLedgerEntry> AddAsync(GeneralLedgerEntry entry);
        Task<IEnumerable<GeneralLedgerEntry>> GetNonReversalByReferenceAsync(int businessLocationId, string referenceType, long referenceId);
        Task<HashSet<long>> GetReversedEntryIdsAsync(IEnumerable<long> entryIds);
        Task<List<(long EntryId, long? ReversedEntryId)>> GetEntryLinksByIdsAsync(IEnumerable<long> entryIds);
        Task<Dictionary<long, (int ReversalCount, long? LatestReversalEntryId)>> GetReversalSummaryAsOfAsync(
            IEnumerable<long> entryIds,
            DateOnly asOfDate);

        // ── Formula Engine aggregation (avoids PageSize = int.MaxValue) ──
        Task<decimal> AggregateByLocationAndPeriodAsync(
            int locationId, DateOnly from, DateOnly to,
            string aggType, string field,
            string? moneyChannel, string? transactionType);
    }
}
