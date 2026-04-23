using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Revenue;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IRevenueService
    {
        Task<RevenueDto> CreateManualAsync(Guid userId, CreateManualRevenueRequest request);
        Task<ManualRevenueUpdateResponseDto> UpdateManualAsync(Guid userId, long revenueId, UpdateManualRevenueRequest request);
        Task<PaginatedResponse<RevenueDto>> ListAsync(Guid userId, RevenueQueryParams query);
        Task DeleteManualAsync(Guid userId, long revenueId);
    }
}
