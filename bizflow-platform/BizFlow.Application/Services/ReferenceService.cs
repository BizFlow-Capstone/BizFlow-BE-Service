using BizFlow.Application.Common.Constants;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services
{
    public class ReferenceService : IReferenceService
    {
        public IReadOnlyList<string> GetPaymentMethods() => PaymentMethods.ExposedToUser;

        public IReadOnlyList<string> GetBusinessTypeStatuses() => BusinessTypeStatus.All;

        public IReadOnlyList<string> GetCostTypes() => CostType.All;

        public IReadOnlyList<string> GetGeneralLedgerReferenceTypes() => GeneralLedgerReferenceType.All;

        public IReadOnlyList<string> GetGeneralLedgerTransactionTypes() => GeneralLedgerTransactionType.All;

        public IReadOnlyList<string> GetGeneralLedgerViewModes() => GeneralLedgerViewMode.All;

        public IReadOnlyList<string> GetImportStatuses() => ImportStatus.All;

        public IReadOnlyList<string> GetImportTypes() => ImportType.All;

        public IReadOnlyList<string> GetMoneyChannelTypes() => MoneyChannelType.All;

        public IReadOnlyList<string> GetOrderStatuses() => OrderStatus.All;

        public IReadOnlyList<string> GetProductStatuses() => ProductStatus.All;

        public IReadOnlyList<string> GetRevenueTypes() => RevenueType.All;

        public IReadOnlyList<string> GetSubscriptionStatuses() => SubscriptionStatus.All;

        public IReadOnlyList<string> GetSubscriptionTransactionTypes() => TransactionType.All;

        public IReadOnlyList<string> GetStockMovementTypes() => StockMovementType.All;

        public IReadOnlyList<string> GetStockMovementReferenceTypes() => StockMovementReferenceType.All;
    }
}