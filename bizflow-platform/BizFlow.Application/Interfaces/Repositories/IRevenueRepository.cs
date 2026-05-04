using BizFlow.Application.DTOs.Revenue;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IRevenueRepository
    {
        Task<(IEnumerable<Revenue> Items, int TotalCount)> SearchAsync(RevenueQueryParams query);
        Task<Revenue?> GetByIdAsync(long revenueId);
        Task<List<Revenue>> GetSaleByOrderIdAsync(int businessLocationId, long orderId);

        /// <summary>True when an append-only reversal row already offsets this revenue.</summary>
        Task<bool> HasActiveReversalForRevenueAsync(long revenueId, CancellationToken cancellationToken = default);

        Task<IEnumerable<Revenue>> GetByIdsAsync(IEnumerable<long> revenueIds);
        Task<Revenue> AddAsync(Revenue revenue);
        void Update(Revenue revenue);

        // ── DocumentNumber uniqueness helpers (replace-when-posted flow) ──

        /// <summary>
        /// Must run inside a DB transaction. Acquires an InnoDB row-level write
        /// lock on the target Revenue row via <c>SELECT ... FOR UPDATE</c>.
        /// </summary>
        Task LockRevenueRowForUpdateAsync(long revenueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if any Revenue row (including soft-deleted, cancelled or
        /// replaced) in the given BusinessLocation uses the supplied normalized
        /// document number.
        /// </summary>
        Task<bool> ExistsByDocumentNumberAsync(
            int businessLocationId,
            string documentNumberNormalized,
            long? excludeRevenueId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Variant that checks across all locations belonging to the supplied
        /// owner. Used for the "unique-per-owner, cross-location" rule.
        /// </summary>
        Task<bool> ExistsByDocumentNumberForOwnerAsync(
            Guid ownerId,
            string documentNumberNormalized,
            long? excludeRevenueId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the most recent replacement Revenue that references the given
        /// original RevenueId via <c>RefRevenueId</c>.
        /// </summary>
        Task<Revenue?> GetLatestReplacementByRefRevenueIdAsync(long refRevenueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Idempotent variant keyed by <c>IdempotencyKey</c>.
        /// </summary>
        Task<Revenue?> GetLatestReplacementByRefRevenueIdAsync(
            long refRevenueId,
            string idempotencyKey,
            CancellationToken cancellationToken = default);

        Task<decimal> SumAmountByLocationsAndDateRangeAsync(
            IReadOnlyCollection<int> businessLocationIds,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default);

        // ── Formula Engine aggregation (avoids PageSize = int.MaxValue) ──
        Task<decimal> AggregateByLocationAndPeriodAsync(
            int locationId, DateOnly from, DateOnly to,
            string aggType, string[] revenueTypes);

        Task<Dictionary<string, decimal>> SumGroupedByBusinessTypeAsync(
            int locationId, DateOnly from, DateOnly to);

        /// <summary>
        /// True when a reversal row already references this revenue id (idempotency).
        /// </summary>
        Task<bool> HasReversalForOriginalRevenueAsync(
            long originalRevenueId,
            CancellationToken cancellationToken = default);
    }
}
