namespace BizFlow.Application.DTOs.Subscription
{
    public class CheckoutSessionResponseDto
    {
        public Guid TransactionId { get; set; }
        public string SessionUrl { get; set; } = string.Empty;
        public decimal PlanPrice { get; set; }
        public decimal ProrationCredit { get; set; }
        public decimal FinalAmount { get; set; }
        public string Currency { get; set; } = "VND";
        public string TransactionType { get; set; } = string.Empty;
    }
}
