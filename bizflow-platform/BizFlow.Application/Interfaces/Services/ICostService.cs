using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Cost;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Services
{
    public interface ICostService
    {
        Task<CostDto> CreateManualAsync(Guid userId, CreateManualCostRequest request);
        Task<CostDto> UpdateManualAsync(Guid userId, long costId, UpdateManualCostRequest request);
        Task<PaginatedResponse<CostDto>> ListAsync(Guid userId, CostQueryParams query);
        Task DeleteManualAsync(Guid userId, long costId);

        Task<Cost> CreateImportCostAsync(Guid userId, Import import);
        Task ReverseImportCostAsync(Guid userId, Import import, string? reason = null);
    }
}
