using BizFlow.Application.DTOs.Reference;

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
        /// <summary>
        /// Subscription transaction type. <c>Code</c> ∈ {<c>PURCHASE</c>, <c>RENEW</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto TransactionType { get; set; } = null!;
    }
}
