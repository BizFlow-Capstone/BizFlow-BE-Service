using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    /// <summary>
    /// Định kỳ: khi qua DiscountStart/DiscountEnd, cập nhật cờ giảm giá trên DB và
    /// đồng bộ Stripe Price với giá hiệu dụng (Checkout dùng StripePriceId cố định).
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
