namespace BizFlow.Application.Common.Models
{
    public class StripeSettings
    {
        public const string SectionName = "StripeSettings";

        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;

        /// <summary>Backend callback URL — Stripe redirects here, then backend routes to web/mobile.</summary>
        public string SuccessCallbackUrl { get; set; } = string.Empty;
        public string CancelCallbackUrl { get; set; } = string.Empty;

        public string WebSuccessUrl { get; set; } = string.Empty;
        public string WebCancelUrl { get; set; } = string.Empty;
        public string MobileSuccessUrl { get; set; } = string.Empty;
        public string MobileCancelUrl { get; set; } = string.Empty;
    }
}
