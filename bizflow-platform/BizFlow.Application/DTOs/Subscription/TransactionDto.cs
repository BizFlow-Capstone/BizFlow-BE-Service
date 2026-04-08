namespace BizFlow.Application.DTOs.Subscription
{
    public class TransactionDto
    {
        public Guid TransactionId { get; set; }
        public int SubscriptionPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal PlanPrice { get; set; }
        public decimal ProrationCredit { get; set; }
        public decimal FinalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
