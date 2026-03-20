namespace BizFlow.Application.Interfaces.Services
{
    public interface IReferenceService
    {
        IReadOnlyList<string> GetPaymentMethods();
        IReadOnlyList<string> GetBusinessTypeStatuses();
        IReadOnlyList<string> GetCostTypes();
        IReadOnlyList<string> GetGeneralLedgerReferenceTypes();
        IReadOnlyList<string> GetGeneralLedgerTransactionTypes();
        IReadOnlyList<string> GetGeneralLedgerViewModes();
        IReadOnlyList<string> GetImportStatuses();
        IReadOnlyList<string> GetImportTypes();
        IReadOnlyList<string> GetMoneyChannelTypes();
        IReadOnlyList<string> GetOrderStatuses();
        IReadOnlyList<string> GetProductStatuses();
        IReadOnlyList<string> GetRevenueTypes();
        IReadOnlyList<string> GetStockMovementTypes();
        IReadOnlyList<string> GetStockMovementReferenceTypes();
    }
}