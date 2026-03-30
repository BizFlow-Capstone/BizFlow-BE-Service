using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    /// <summary>
    /// Periodically pulls recent Stripe refunds to heal missed webhook processing.
    /// </summary>
    public class StripeRefundReconcileJob
    {
        private readonly IStripeService _stripeService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<StripeRefundReconcileJob> _logger;

        public StripeRefundReconcileJob(
            IStripeService stripeService,
            ISubscriptionService subscriptionService,
            ILogger<StripeRefundReconcileJob> logger)
        {
            _stripeService = stripeService;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var fromUtc = DateTime.UtcNow.AddDays(-2);
            var refunds = await _stripeService.ListRecentRefundsAsync(fromUtc, limit: 100);

            var synced = 0;
            foreach (var refund in refunds)
            {
                if (string.IsNullOrWhiteSpace(refund.PaymentIntentId))
                {
                    continue;
                }

                await _subscriptionService.HandleChargeRefundedAsync(refund.PaymentIntentId);
                synced++;
            }

            _logger.LogInformation(
                "StripeRefundReconcileJob checked {Total} refunds and synchronized {Synced} records",
                refunds.Count,
                synced);
        }
    }
}
