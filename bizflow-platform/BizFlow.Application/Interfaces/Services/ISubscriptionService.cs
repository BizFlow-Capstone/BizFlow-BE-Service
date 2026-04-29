using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Subscription;

namespace BizFlow.Application.Interfaces.Services
{
    public interface ISubscriptionService
    {
        Task EnsureFreePlanSetupAsync();
        Task EnsureFreeSubscriptionAsync(Guid ownerProfileId);
        Task<int> EnsureFreeSubscriptionsForOwnersWithoutActiveAsync();
        /// <summary>Checks whether the signed-in user currently has an active subscription.</summary>
        Task<bool> HasActiveSubscriptionAsync(Guid ownerProfileId);
        /// <summary>Renews free-plan cycle: resets usage and extends EndDate to next cycle.</summary>
        Task RenewFreeSubscriptionCycleAsync(Guid subscriptionId);
        Task<CurrentSubscriptionDto> GetCurrentSubscriptionAsync(Guid profileId);
        Task<CurrentSubscriptionDto> GetCurrentSubscriptionByLocationAsync(Guid profileId, int locationId);
        Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(Guid profileId, int subscriptionPlanId, string platform = "web", int quantity = 1);
        Task RevokeAccessGrantAsync(Guid ownerProfileId, Guid memberProfileId);
        Task<List<TransactionDto>> GetTransactionsAsync(Guid profileId, int page = 1, int pageSize = 20);
        Task<AdminSubscriptionAnalyticsResponse> GetAdminAnalyticsAsync(AdminSubscriptionAnalyticsQuery query, CancellationToken cancellationToken = default);

        Task HandleCheckoutCompletedAsync(StripeCheckoutSessionPayload session);
        Task HandleCheckoutExpiredAsync(StripeCheckoutSessionPayload session);
        Task HandlePaymentFailedAsync(StripePaymentIntentPayload paymentIntent);
        Task HandleChargeRefundedAsync(string paymentIntentId);

        Task<bool> CheckFeatureAccessAsync(Guid profileId, int locationId, string featureCode, bool incrementUsage = false);
        Task<bool> CheckFeatureAccessByOwnerAsync(Guid ownerProfileId, string featureCode, bool incrementUsage = false);

        Task<FeatureAccessEvaluationResult> EvaluateFeatureAccessAsync(Guid profileId, int locationId, string featureCode);

        Task<FeatureAccessEvaluationResult> EvaluateFeatureAccessByOwnerAsync(Guid ownerProfileId, string featureCode);
        Task IncrementUsageSqlBackgroundAsync(Guid subscriptionId, string featureCode);
        Task<string> GetPaymentRedirectUrlAsync(string? sessionId, bool isSuccess);
    }
}
