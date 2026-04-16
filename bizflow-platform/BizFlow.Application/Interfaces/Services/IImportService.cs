using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Import;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IImportService
    {
        /// <summary>
        /// Get active import JSON schema template
        /// </summary>
        Task<ImportSchemaDto> GetTemplateAsync();

        /// <summary>
        /// Create a new import (status = DRAFT)
        /// </summary>
        Task<ImportSummaryDto> CreateImportAsync(Guid userId, CreateImportRequest request);

        /// <summary>
        /// Update a DRAFT import (fields + items)
        /// </summary>
        Task<ImportSummaryDto> UpdateImportAsync(Guid userId, long importId, UpdateImportRequest request);

        /// <summary>
        /// Confirm or cancel an import.
        /// CONFIRMED → adds quantity to product stock.
        /// </summary>
        Task<ImportPatchResultDto> PatchImportAsync(Guid userId, long importId, PatchImportRequest request);

        /// <summary>
        /// Get paginated list of imports with filters
        /// </summary>
        Task<PaginatedResponse<ImportSummaryDto>> ListImportsAsync(Guid userId, ImportQueryParams query);

        /// <summary>
        /// Get full import detail including items
        /// </summary>
        Task<ImportDetailDto> GetImportDetailAsync(Guid userId, long importId);

        /// <summary>
        /// Create a confirmed inventory-adjustment import with one item.
        /// Used by Product flow for stock and/or cost-price adjustments.
        /// Quantity can be 0 for cost-price-only updates.
        /// Returns created ImportId.
        /// </summary>
        Task<long> CreateInventoryAdjustmentImportAsync(int businessLocationId, long productId, decimal quantity, decimal costPrice, string? memo = null);

        /// <summary>
        /// Delete import.
        /// DRAFT → hard-delete directly.
        /// CONFIRMED → subtract stock per item, then hard-delete.
        /// </summary>
        Task DeleteImportAsync(Guid userId, long importId);
    }
}
