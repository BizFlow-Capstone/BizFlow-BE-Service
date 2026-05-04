using BizFlow.Application.DTOs.Debtor;
using BizFlow.Application.Common.Models;
using BizFlow.Domain.Entities;

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
        Task<DebtorBalanceSyncResultDto> SyncCurrentBalancesAsync(long? debtorId = null, CancellationToken cancellationToken = default);
        Task<DebtorPaymentTransaction> RecordSystemDebtIncreaseAsync(Guid userId, long debtorId, decimal amount, string note);
        Task<DebtorPaymentTransaction> RecordSystemDebtRollbackAsync(Guid userId, long debtorId, decimal amount, string note);

        /// <summary>
        /// Writes a system debt-rollback line to the general ledger (e.g. order cancel). Transaction must already be persisted. Caller owns SaveChanges after.
        /// </summary>
        Task RecordSystemDebtRollbackLedgerEntryAsync(
            DebtorPaymentTransaction persistedRollback,
            int businessLocationId,
            CancellationToken cancellationToken = default);
    }
}
