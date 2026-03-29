namespace BizFlow.Application.DTOs.Subscription
{
    public class SubscriptionAccessGrantRequest
    {
        public Guid MemberProfileId { get; set; }
        public bool CanReadUsage { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}
