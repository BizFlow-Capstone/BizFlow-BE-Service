namespace BizFlow.Application.DTOs.Subscription
{
    public class AdminSubscriptionPlanSummaryDto
    {
        public int SubscriptionPlanId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int DurationDays { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }
        public bool IsDiscountPeriodActive { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
