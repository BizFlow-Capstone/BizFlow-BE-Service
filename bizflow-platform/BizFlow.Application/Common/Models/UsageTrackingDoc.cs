namespace BizFlow.Application.Common.Models
{
    public class UsageFeatureData
    {
        public int Used { get; set; }
        public int Limit { get; set; }
    }

    public class UsageTrackingDoc
    {
        public string OwnerProfileId { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string SubscriptionId { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "active";
        public Dictionary<string, UsageFeatureData> Features { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
    }

    public class FeatureUsageSnapshot
    {
        public int Used { get; set; }
        public int Limit { get; set; }
    }
}
