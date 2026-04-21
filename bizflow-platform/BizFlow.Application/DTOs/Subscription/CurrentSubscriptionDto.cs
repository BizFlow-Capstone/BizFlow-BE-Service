using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Subscription
{
    public class CurrentSubscriptionDto
    {
        public Guid? SubscriptionId { get; set; }
        /// <summary>
        /// Subscription status. <c>Code</c> ∈ {<c>Pending</c>, <c>Active</c>, <c>Expired</c>,
        /// <c>Upgraded</c>, <c>Cancelled</c>, <c>Inactive</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SubscriptionPlanDto? Plan { get; set; }
    }
}
