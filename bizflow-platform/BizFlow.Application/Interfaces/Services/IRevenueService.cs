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
        /// Inserts an append-only reversal row after GL reversal (idempotent). Call inside the same unit-of-work transaction as the cancel.
        /// </summary>
        Task AppendReversalRowAfterGlReverseAsync(
            Revenue original,
            Guid userId,
            string reversalMessageKey,
            CancellationToken cancellationToken = default);
    }
}
