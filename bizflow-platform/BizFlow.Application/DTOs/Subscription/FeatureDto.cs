namespace BizFlow.Application.DTOs.Subscription
{
    public class FeatureDto
    {
        public int FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
