namespace BizFlow.Application.DTOs.Subscription
{
    /// <summary>
    /// Full plan data for admin view — raw values, includes Stripe IDs.
    /// </summary>
    public class AdminSubscriptionPlanDto
    {
        public int SubscriptionPlanId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationDays { get; set; }
        public bool IsActive { get; set; }
        public string? StripeProductId { get; set; }
        public string? StripePriceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public AdminSubscriptionPlanPriceDto? CurrentPrice { get; set; }
        public List<PlanFeatureDto> Features { get; set; } = new();
    }
}
