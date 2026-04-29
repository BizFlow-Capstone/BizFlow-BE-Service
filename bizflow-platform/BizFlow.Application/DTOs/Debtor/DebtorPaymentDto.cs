using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Debtor
{
    public class DebtorPaymentDto
    {
        public long TransactionId { get; set; }
        public long DebtorId { get; set; }
        public decimal Amount { get; set; }
        /// <summary>
        /// Payment method. <c>Code</c> ∈ {<c>cash</c>, <c>bank</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto PaymentMethod { get; set; } = null!;
        public string? Notes { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal BalanceDelta { get; set; }
        public ReferenceOptionDto DebtDirection { get; set; } = null!;
        public string DebtAction { get; set; } = null!;
        public decimal OutstandingDebtAfter { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
