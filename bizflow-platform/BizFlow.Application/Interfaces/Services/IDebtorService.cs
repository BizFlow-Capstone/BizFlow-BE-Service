using BizFlow.Application.DTOs.Debtor;
using BizFlow.Application.Common.Models;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IDebtorService
    {
        Task<PaginatedResponse<DebtorSummaryDto>> ListAsync(Guid userId, DebtorQueryParams query);
        Task<DebtorDetailDto> GetDetailAsync(Guid userId, long debtorId);
        Task<DebtorDetailDto> CreateAsync(Guid userId, CreateDebtorRequest request);
        Task<DebtorDetailDto> UpdateAsync(Guid userId, long debtorId, UpdateDebtorRequest request);
        Task<DebtorDetailDto> UpdateStatusAsync(Guid userId, long debtorId, bool isActive);
        Task DeleteAsync(Guid userId, long debtorId, bool forceDelete = false);
        Task<IEnumerable<DebtorMinimalDto>> GetActiveDebtorsByLocationAsync(Guid userId, int locationId);
        Task<DebtorPaymentDto> RecordPaymentAsync(Guid userId, long debtorId, RecordDebtPaymentRequest request);
        Task<IEnumerable<DebtorPaymentDto>> GetPaymentsAsync(Guid userId, long debtorId);
    }
}
