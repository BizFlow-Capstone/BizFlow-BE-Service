using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Cost;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Services
{
    public interface ICostService
    {
        Task<CostDto> CreateManualAsync(Guid userId, CreateManualCostRequest request);
        Task<ManualCostUpdateResponseDto> UpdateManualAsync(Guid userId, long costId, UpdateManualCostRequest request);
        Task<PaginatedResponse<CostDto>> ListAsync(Guid userId, CostQueryParams query);
        Task DeleteManualAsync(Guid userId, long costId);

        /// <summary>
        /// Creates (or revives) the import-backed Cost inside its own resilient transaction.
        /// </summary>
        Task<Cost> CreateImportCostAsync(Guid userId, Import import, string? documentNumber = null, DateOnly? documentDate = null);

        /// <summary>
        /// Same as <see cref="CreateImportCostAsync"/> but must be called only while already
        /// inside <c>IUnitOfWork.ExecuteResilientAsync</c> (e.g. import replacement).
        /// </summary>
        Task<Cost> CreateImportCostInCurrentTransactionAsync(
            Guid userId,
            Import import,
            string? documentNumber = null,
            DateOnly? documentDate = null,
            CancellationToken cancellationToken = default);

        Task ReverseImportCostAsync(Guid userId, Import import, string? reason = null);
    }
}
