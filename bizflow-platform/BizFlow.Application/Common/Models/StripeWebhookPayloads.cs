namespace BizFlow.Application.Common.Models
{
    public class StripeCheckoutSessionPayload
    {
        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Status { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class StripePaymentIntentPayload
    {
        public string? PaymentIntentId { get; set; }
        public string? Status { get; set; }
    }
}

