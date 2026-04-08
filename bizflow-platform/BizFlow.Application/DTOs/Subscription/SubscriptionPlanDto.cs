namespace BizFlow.Application.DTOs.Subscription
{
    public class SubscriptionPlanDto
    {
        public int SubscriptionPlanId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationDays { get; set; }
        public SubscriptionPlanPriceDto? CurrentPrice { get; set; }
        public List<PlanFeatureDto> Features { get; set; } = new();
    }
}
