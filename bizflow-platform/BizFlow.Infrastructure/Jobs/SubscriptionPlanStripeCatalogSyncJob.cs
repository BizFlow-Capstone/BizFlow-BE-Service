using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    /// <summary>
    /// Scheduled job: when crossing DiscountStart/DiscountEnd, update discount flags in the DB and
    /// sync the Stripe Price to the effective amount (Checkout uses a fixed StripePriceId).
    /// </summary>
    public class SubscriptionPlanStripeCatalogSyncJob
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly ILogger<SubscriptionPlanStripeCatalogSyncJob> _logger;

        public SubscriptionPlanStripeCatalogSyncJob(
            ISubscriptionPlanService subscriptionPlanService,
            ILogger<SubscriptionPlanStripeCatalogSyncJob> logger)
        {
            _subscriptionPlanService = subscriptionPlanService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await _subscriptionPlanService.ReconcileDiscountWindowsAndStripeCatalogAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubscriptionPlanStripeCatalogSyncJob failed");
                throw;
            }
        }
    }
}
