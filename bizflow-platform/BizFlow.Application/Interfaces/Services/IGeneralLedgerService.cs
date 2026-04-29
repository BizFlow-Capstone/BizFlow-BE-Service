using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IGeneralLedgerService
    {
        Task<PaginatedResponse<GeneralLedgerEntryDto>> ListAsync(Guid userId, GeneralLedgerQueryParams query);

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
    }
}
