namespace BizFlow.Application.DTOs.Debtor
{
    public class DebtorPaymentDto
    {
        public long TransactionId { get; set; }
        public long DebtorId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string? Notes { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
