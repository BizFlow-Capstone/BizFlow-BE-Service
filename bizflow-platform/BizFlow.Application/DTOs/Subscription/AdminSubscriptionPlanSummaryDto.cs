namespace BizFlow.Application.DTOs.Subscription
{
    public class AdminSubscriptionPlanSummaryDto
    {
        public int SubscriptionPlanId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int DurationDays { get; set; }
        public decimal? BasePrice { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
