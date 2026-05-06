namespace BizFlow.Application.DTOs.Subscription
{
    public class PlanFeatureDto
    {
        public int FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public int UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public string FeatureDescription { get; set; } = string.Empty;
    }
}
