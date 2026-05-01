using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Reference;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services
{
    public class ReferenceService : IReferenceService
    {
        private readonly IReferenceLabelService _labelService;

        public ReferenceService(IReferenceLabelService labelService)
        {
            _labelService = labelService;
        }

        public IReadOnlyList<ReferenceOptionDto> GetPaymentMethods() =>
            Build(ReferenceCategory.PaymentMethod, PaymentMethods.ExposedToUser);

        public IReadOnlyList<ReferenceOptionDto> GetBusinessTypeStatuses() =>
            Build(ReferenceCategory.BusinessTypeStatus, BusinessTypeStatus.All);

        public IReadOnlyList<ReferenceOptionDto> GetCostTypes() =>
            Build(ReferenceCategory.CostType, CostType.All);

        public IReadOnlyList<ReferenceOptionDto> GetCostStatuses() =>
            Build(ReferenceCategory.CostStatus, CostStatus.All);

        public IReadOnlyList<ReferenceOptionDto> GetRevenueStatuses() =>
            Build(ReferenceCategory.RevenueStatus, RevenueStatus.All);

        public IReadOnlyList<ReferenceOptionDto> GetDebtDirections() =>
            Build(ReferenceCategory.DebtDirection, DebtDirection.All);

        public IReadOnlyList<ReferenceOptionDto> GetGeneralLedgerReferenceTypes() =>
            Build(ReferenceCategory.GeneralLedgerReferenceType, GeneralLedgerReferenceType.All);

        public IReadOnlyList<ReferenceOptionDto> GetGeneralLedgerTransactionTypes() =>
            Build(ReferenceCategory.GeneralLedgerTransactionType, GeneralLedgerTransactionType.All);

        public IReadOnlyList<ReferenceOptionDto> GetGeneralLedgerViewModes() =>
            Build(ReferenceCategory.GeneralLedgerViewMode, GeneralLedgerViewMode.All);

        public IReadOnlyList<ReferenceOptionDto> GetLedgerEffectiveStatuses() =>
            Build(ReferenceCategory.LedgerEffectiveStatus, LedgerEffectiveStatuses.All);

        public IReadOnlyList<ReferenceOptionDto> GetImportStatuses() =>
            Build(ReferenceCategory.ImportStatus, ImportStatus.All);

        public IReadOnlyList<ReferenceOptionDto> GetImportTypes() =>
            Build(ReferenceCategory.ImportType, ImportType.All);

        public IReadOnlyList<ReferenceOptionDto> GetHireStatuses() =>
            Build(ReferenceCategory.HireStatus, HireStatuses.All);

        public IReadOnlyList<ReferenceOptionDto> GetAccountingPeriodTypes() =>
            Build(ReferenceCategory.AccountingPeriodType, AccountingPeriodConstants.PeriodTypes.All);

        public IReadOnlyList<ReferenceOptionDto> GetAccountingPeriodStatuses() =>
            Build(ReferenceCategory.AccountingPeriodStatus, AccountingPeriodConstants.PeriodStatuses.All);

        public IReadOnlyList<ReferenceOptionDto> GetAccountingPeriodAuditActions() =>
            Build(ReferenceCategory.AccountingPeriodAuditAction, AccountingPeriodConstants.AuditActions.All);

        public IReadOnlyList<ReferenceOptionDto> GetMoneyChannelTypes() =>
            Build(ReferenceCategory.MoneyChannelType, MoneyChannelType.ExposedToUser);

        public IReadOnlyList<ReferenceOptionDto> GetOrderStatuses() =>
            Build(ReferenceCategory.OrderStatus, OrderStatus.All);

        public IReadOnlyList<ReferenceOptionDto> GetProductStatuses() =>
            Build(ReferenceCategory.ProductStatus, ProductStatus.All);

        public IReadOnlyList<ReferenceOptionDto> GetRevenueTypes() =>
            Build(ReferenceCategory.RevenueType, RevenueType.All);

        public IReadOnlyList<ReferenceOptionDto> GetSubscriptionStatuses() =>
            Build(ReferenceCategory.SubscriptionStatus, SubscriptionStatus.All);

        public IReadOnlyList<ReferenceOptionDto> GetSubscriptionTransactionTypes() =>
            Build(ReferenceCategory.SubscriptionTransactionType, TransactionType.All);

        public IReadOnlyList<ReferenceOptionDto> GetTransactionStatuses() =>
            Build(ReferenceCategory.TransactionStatus, TransactionStatus.All);

        public IReadOnlyList<ReferenceOptionDto> GetStockMovementTypes() =>
            Build(ReferenceCategory.StockMovementType, StockMovementType.All);

        public IReadOnlyList<ReferenceOptionDto> GetStockMovementReferenceTypes() =>
            Build(ReferenceCategory.StockMovementReferenceType, StockMovementReferenceType.All);

        private IReadOnlyList<ReferenceOptionDto> Build(string category, IReadOnlyList<string> codes)
        {
            var result = new List<ReferenceOptionDto>(codes.Count);
            foreach (var code in codes)
            {
                result.Add(new ReferenceOptionDto(code, _labelService.GetLabel(category, code)));
            }
            return result;
        }
    }
}
