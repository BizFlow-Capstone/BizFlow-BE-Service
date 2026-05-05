using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IGeneralLedgerService
    {
        Task<PaginatedResponse<GeneralLedgerEntryDto>> ListAsync(Guid userId, GeneralLedgerQueryParams query);

        /// <summary>
        /// Tổng doanh thu / chi phí chỉ từ sổ cái (reference_type revenue | cost), không đọc bảng Revenue/Cost.
        /// </summary>
        Task<GeneralLedgerTotalsDto> GetTotalsAsync(Guid userId, GeneralLedgerQueryParams query);

        Task<GeneralLedgerEntry> RecordImportCostAsync(Cost cost);
        Task<GeneralLedgerEntry> RecordManualCostAsync(Cost cost);
        Task<int> ReverseCostEntriesAsync(Cost cost, string reversalReason);

        Task<GeneralLedgerEntry> RecordDebtPaymentAsync(
            DebtorPaymentTransaction transaction,
            int businessLocationId,
            string? debtAction = null);

        Task<GeneralLedgerEntry> RecordSaleRevenueAsync(Revenue revenue);
        Task<GeneralLedgerEntry> RecordManualRevenueAsync(Revenue revenue);
        Task<int> ReverseRevenueEntriesAsync(Revenue revenue, string reversalReason);

        /// <summary>
        /// One GL line per <see cref="Revenue"/> row (supports negative <see cref="Revenue.Amount"/> as credit).
        /// </summary>
        Task<GeneralLedgerEntry> RecordRevenueLedgerLineFromRowAsync(Revenue revenue);

        /// <summary>
        /// One GL line per <see cref="Cost"/> row (supports negative <see cref="Cost.Amount"/> as debit).
        /// </summary>
        Task<GeneralLedgerEntry> RecordCostLedgerLineFromRowAsync(Cost cost);
    }
}
