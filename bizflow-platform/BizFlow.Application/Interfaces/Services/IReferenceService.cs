using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IReferenceService
    {
        IReadOnlyList<ReferenceOptionDto> GetPaymentMethods();
        IReadOnlyList<ReferenceOptionDto> GetBusinessTypeStatuses();
        IReadOnlyList<ReferenceOptionDto> GetCostTypes();
        IReadOnlyList<ReferenceOptionDto> GetGeneralLedgerReferenceTypes();
        IReadOnlyList<ReferenceOptionDto> GetGeneralLedgerTransactionTypes();
        IReadOnlyList<ReferenceOptionDto> GetGeneralLedgerViewModes();
        IReadOnlyList<ReferenceOptionDto> GetImportStatuses();
        IReadOnlyList<ReferenceOptionDto> GetImportTypes();
        IReadOnlyList<ReferenceOptionDto> GetMoneyChannelTypes();
        IReadOnlyList<ReferenceOptionDto> GetOrderStatuses();
        IReadOnlyList<ReferenceOptionDto> GetProductStatuses();
        IReadOnlyList<ReferenceOptionDto> GetRevenueTypes();
        IReadOnlyList<ReferenceOptionDto> GetSubscriptionStatuses();
        IReadOnlyList<ReferenceOptionDto> GetSubscriptionTransactionTypes();
        IReadOnlyList<ReferenceOptionDto> GetStockMovementTypes();
        IReadOnlyList<ReferenceOptionDto> GetStockMovementReferenceTypes();
    }
}
