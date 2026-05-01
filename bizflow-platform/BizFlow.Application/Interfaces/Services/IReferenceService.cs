using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IReferenceService
    {
        IReadOnlyList<ReferenceOptionDto> GetPaymentMethods();
        IReadOnlyList<ReferenceOptionDto> GetBusinessTypeStatuses();
        IReadOnlyList<ReferenceOptionDto> GetCostTypes();
        IReadOnlyList<ReferenceOptionDto> GetCostStatuses();
        IReadOnlyList<ReferenceOptionDto> GetRevenueStatuses();
        IReadOnlyList<ReferenceOptionDto> GetDebtDirections();
        IReadOnlyList<ReferenceOptionDto> GetGeneralLedgerReferenceTypes();
        IReadOnlyList<ReferenceOptionDto> GetGeneralLedgerTransactionTypes();
        IReadOnlyList<ReferenceOptionDto> GetGeneralLedgerViewModes();
        IReadOnlyList<ReferenceOptionDto> GetLedgerEffectiveStatuses();
        IReadOnlyList<ReferenceOptionDto> GetImportStatuses();
        IReadOnlyList<ReferenceOptionDto> GetImportTypes();
        IReadOnlyList<ReferenceOptionDto> GetHireStatuses();
        IReadOnlyList<ReferenceOptionDto> GetAccountingPeriodTypes();
        IReadOnlyList<ReferenceOptionDto> GetAccountingPeriodStatuses();
        IReadOnlyList<ReferenceOptionDto> GetAccountingPeriodAuditActions();
        IReadOnlyList<ReferenceOptionDto> GetMoneyChannelTypes();
        IReadOnlyList<ReferenceOptionDto> GetOrderStatuses();
        IReadOnlyList<ReferenceOptionDto> GetProductStatuses();
        IReadOnlyList<ReferenceOptionDto> GetRevenueTypes();
        IReadOnlyList<ReferenceOptionDto> GetSubscriptionStatuses();
        IReadOnlyList<ReferenceOptionDto> GetSubscriptionTransactionTypes();
        IReadOnlyList<ReferenceOptionDto> GetTransactionStatuses();
        IReadOnlyList<ReferenceOptionDto> GetStockMovementTypes();
        IReadOnlyList<ReferenceOptionDto> GetStockMovementReferenceTypes();
    }
}
