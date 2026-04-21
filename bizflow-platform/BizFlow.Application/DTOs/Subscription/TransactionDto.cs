using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Subscription
{
    public class TransactionDto
    {
        public Guid TransactionId { get; set; }
        public int SubscriptionPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        /// <summary>
        /// Subscription transaction type. <c>Code</c> ∈ {<c>PURCHASE</c>, <c>RENEW</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto TransactionType { get; set; } = null!;
        /// <summary>
        /// Transaction status. <c>Code</c> ∈ {<c>Pending</c>, <c>Success</c>, <c>Failed</c>, <c>Refunded</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
        public decimal PlanPrice { get; set; }
        public decimal ProrationCredit { get; set; }
        public decimal FinalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
