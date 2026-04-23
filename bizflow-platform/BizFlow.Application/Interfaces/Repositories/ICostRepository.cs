using BizFlow.Application.DTOs.Cost;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ICostRepository
    {
        Task<(IEnumerable<Cost> Items, int TotalCount)> SearchAsync(CostQueryParams query);
        Task<Cost?> GetByIdAsync(long costId);
        Task<Cost?> GetByImportIdAsync(long importId);
        Task<IEnumerable<Cost>> GetByIdsAsync(IEnumerable<long> costIds);
        Task<Cost> AddAsync(Cost cost);
        void Update(Cost cost);

        // ── DocumentNumber uniqueness helpers (replace-when-posted flow) ──

        /// <summary>
        /// Must run inside a DB transaction. Acquires an InnoDB row-level write
        /// lock on the target Cost row via <c>SELECT ... FOR UPDATE</c>.
        /// </summary>
        Task LockCostRowForUpdateAsync(long costId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if any Cost row (including soft-deleted, cancelled or replaced)
        /// in the given BusinessLocation uses the supplied normalized document
        /// number. <paramref name="excludeCostId"/> lets callers exclude the
        /// current record (useful when updating in-place).
        /// </summary>
        Task<bool> ExistsByDocumentNumberAsync(
            int businessLocationId,
            string documentNumberNormalized,
            long? excludeCostId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Variant that checks across all locations belonging to the supplied owner.
        /// Used for the "unique-per-owner, cross-location" rule.
        /// </summary>
        Task<bool> ExistsByDocumentNumberForOwnerAsync(
            Guid ownerId,
            string documentNumberNormalized,
            long? excludeCostId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the most recent replacement Cost that references the given
        /// original CostId via <c>RefCostId</c>. Supports idempotent replace-
        /// when-posted flow.
        /// </summary>
        Task<Cost?> GetLatestReplacementByRefCostIdAsync(long refCostId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Idempotent variant: finds a replacement row whose <c>IdempotencyKey</c>
        /// matches <paramref name="idempotencyKey"/> (exact match after trim).
        /// </summary>
        Task<Cost?> GetLatestReplacementByRefCostIdAsync(
            long refCostId,
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
            string aggType);

        Task<Dictionary<string, decimal>> SumGroupedByBusinessTypeAsync(
            int locationId, DateOnly from, DateOnly to);
    }
}
