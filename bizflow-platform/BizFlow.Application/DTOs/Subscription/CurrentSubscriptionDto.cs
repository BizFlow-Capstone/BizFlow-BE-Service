namespace BizFlow.Application.DTOs.Subscription
{
    public class CurrentSubscriptionDto
    {
        public Guid? SubscriptionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SubscriptionPlanDto? Plan { get; set; }
    }
}
