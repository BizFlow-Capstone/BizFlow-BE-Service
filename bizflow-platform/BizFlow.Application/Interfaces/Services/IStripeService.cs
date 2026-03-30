using Stripe;
using Stripe.Checkout;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IStripeService
    {
        // Checkout & Payments
        Task<Session> CreateCheckoutSessionAsync(
            string? stripeCustomerId,
            string stripePriceId,
            Guid transactionId,
            Guid profileId,
            string idempotencyKey,
            string platform = "web",
            int quantity = 1);

        Task<Refund?> RefundPaymentIntentAsync(
            string paymentIntentId,
            string idempotencyKey,
            string? reason = null,
            Dictionary<string, string>? metadata = null);

        Task<Session?> GetCheckoutSessionAsync(string checkoutSessionId);
        Task<PaymentIntent?> GetPaymentIntentAsync(string paymentIntentId);
        Task<IReadOnlyList<Refund>> ListRecentRefundsAsync(DateTime fromUtc, int limit = 100);

        Event ConstructEvent(string json, string stripeSignature, string webhookSecret);

        // Product & Price catalog sync
        bool IsConfigured { get; }
        Task<Product> CreateProductAsync(string name, string? description, Dictionary<string, string>? metadata = null);
        /// <param name="active">Nếu có — đồng bộ active/archived của Product trên Stripe (catalog).</param>
        Task<Product> UpdateProductAsync(string productId, string name, string? description, bool? active = null);
        Task ArchiveProductAsync(string productId);
        Task<Price> CreatePriceAsync(string productId, long unitAmount, string currency = "vnd");
        Task<Price?> GetPriceAsync(string priceId);
        Task<Price> ReactivatePriceAsync(string priceId);
        Task ArchivePriceAsync(string priceId);
    }
}
