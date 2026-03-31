using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Common.Models;

namespace BizFlow.Application.Interfaces.Services
{
    public interface ISubscriptionService
    {
        Task<CurrentSubscriptionDto> GetCurrentSubscriptionAsync(Guid profileId);
        Task<CurrentSubscriptionDto> GetCurrentSubscriptionByLocationAsync(Guid profileId, int locationId);
        Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(Guid profileId, int subscriptionPlanId, string platform = "web", int quantity = 1);
        Task RevokeAccessGrantAsync(Guid ownerProfileId, Guid memberProfileId);
        Task<List<TransactionDto>> GetTransactionsAsync(Guid profileId, int page = 1, int pageSize = 20);

        Task HandleCheckoutCompletedAsync(StripeCheckoutSessionPayload session);
        Task HandleCheckoutExpiredAsync(StripeCheckoutSessionPayload session);
        Task HandlePaymentFailedAsync(StripePaymentIntentPayload paymentIntent);
        Task HandleChargeRefundedAsync(string paymentIntentId);

        Task<bool> CheckFeatureAccessAsync(Guid profileId, int locationId, string featureCode, bool incrementUsage = false);
        Task IncrementUsageSqlBackgroundAsync(Guid subscriptionId, string featureCode);
        Task<string> GetPaymentRedirectUrlAsync(string? sessionId, bool isSuccess);
    }
}
