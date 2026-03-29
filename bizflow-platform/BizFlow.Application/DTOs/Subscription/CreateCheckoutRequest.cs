namespace BizFlow.Application.DTOs.Subscription
{
    public class CreateCheckoutRequest
    {
        public int SubscriptionPlanId { get; set; }
        public int Quantity { get; set; } = 1;

        /// <summary>"web" or "mobile" — determines the redirect target after payment.</summary>
        public string Platform { get; set; } = "web";
    }
}
