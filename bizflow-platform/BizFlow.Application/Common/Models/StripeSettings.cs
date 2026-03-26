namespace BizFlow.Application.Common.Models
{
    public class StripeSettings
    {
        public const string SectionName = "StripeSettings";

        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }
}
