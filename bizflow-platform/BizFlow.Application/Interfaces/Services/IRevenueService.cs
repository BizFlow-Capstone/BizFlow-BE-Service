using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IRevenueService
    {
        Task<RevenueDto> CreateManualAsync(Guid userId, CreateManualRevenueRequest request);
        Task<ManualRevenueUpdateResponseDto> UpdateManualAsync(Guid userId, long revenueId, UpdateManualRevenueRequest request);
        Task<PaginatedResponse<RevenueDto>> ListAsync(Guid userId, RevenueQueryParams query);
        Task DeleteManualAsync(Guid userId, long revenueId);

        /// <summary>
        /// Posts GL sale entries for order-backed revenues already persisted (RevenueId assigned). Caller owns SaveChanges.
        /// </summary>
        Task RecordPostedSaleRevenuesToLedgerAsync(
            IEnumerable<Revenue> revenues,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Append-only reversal rows + one GL line per reversal (order cancel). Caller must not mutate the passed revenue entities.
        /// </summary>
        Task ReversePostedSaleRevenuesLedgerForOrderCancelAsync(
            IEnumerable<Revenue> revenues,
            string reversalMessageKey,
            string supersededStatus,
            Guid? reversalCreatedBy = null,
            CancellationToken cancellationToken = default);
    }
}
