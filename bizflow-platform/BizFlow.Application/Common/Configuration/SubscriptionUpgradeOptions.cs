namespace BizFlow.Application.Common.Configuration
{
    /// <summary>
    /// Upgrade rules for switching between paid plans (not free-to-paid).
    /// </summary>
    public class SubscriptionUpgradeOptions
    {
        public const string SectionName = "SubscriptionUpgrade";

        /// <summary>
        /// Remaining-time credit percent from the current plan (0-100).
        /// </summary>
        public decimal RemainingCreditPercent { get; set; } = 100m;

        /// <summary>
        /// Floor: final payable amount cannot be lower than this fraction
        /// of the target plan catalog total (e.g. 0.2 = 20%).
        /// </summary>
        public decimal MinimumFractionOfTargetPlan { get; set; } = 0.20m;
    }
}
