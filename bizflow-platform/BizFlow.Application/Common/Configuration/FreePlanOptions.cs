namespace BizFlow.Application.Common.Configuration
{
    /// <summary>
    /// Default free plan (DB-only, no Stripe). Billing cycle resets monthly
    /// on day 1 in the configured timezone.
    /// </summary>
    public class FreePlanOptions
    {
        public const string SectionName = "FreePlan";

        /// <summary>Plan name used for lookup/seeding in DB.</summary>
        public string PlanName { get; set; } = "Miễn phí";

        public string PlanDescription { get; set; } =
            "Gói miễn phí mặc định — quota reset vào mùng 1 hàng tháng (theo timezone cấu hình).";

        /// <summary>IANA (Linux/macOS) or Windows time zone id (e.g. Asia/Ho_Chi_Minh).</summary>
        public string BillingTimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";

        /// <summary>
        /// Feature list and usage limits for the free plan.
        /// </summary>
        public List<FreePlanFeatureOption> Features { get; set; } =
        [
            new()
            {
                FeatureCode = "AI",
                UsageLimit = 10
            },
            new()
            {
                FeatureCode = "LOCATIONS",
                UsageLimit = 1
            }
        ];

        /// <summary>Currency used for the zero-price DB record.</summary>
        public string Currency { get; set; } = "VND";

        /// <summary>
        /// 0 = calendar-month cycle (do not use DurationDays for expiry math).
        /// Keeps free plan semantics separate from paid day-based plans.
        /// </summary>
        public int DurationDaysInDatabase { get; set; } = 0;
    }

    public class FreePlanFeatureOption
    {
        public string FeatureCode { get; set; } = string.Empty;
        public int UsageLimit { get; set; }
    }
}
