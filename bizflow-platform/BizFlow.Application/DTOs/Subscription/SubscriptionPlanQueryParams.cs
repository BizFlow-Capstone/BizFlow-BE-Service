using BizFlow.Application.Common.Models;

namespace BizFlow.Application.DTOs.Subscription
{
    /// <summary>
    /// Query parameters for admin subscription plan listing/filtering.
    /// Follows the same pattern as ProductQueryParams.
    /// </summary>
    public class SubscriptionPlanQueryParams : PaginationParams
    {
        /// <summary>
        /// Search by plan name (contains, case-insensitive)
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Filter by status: "active", "archived", or null for all
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter plans created on or after this date
        /// </summary>
        public DateTime? CreatedAtGte { get; set; }

        /// <summary>
        /// Filter plans created on or before this date
        /// </summary>
        public DateTime? CreatedAtLte { get; set; }
    }
}
