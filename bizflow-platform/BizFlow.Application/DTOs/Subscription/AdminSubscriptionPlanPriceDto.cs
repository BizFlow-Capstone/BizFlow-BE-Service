namespace BizFlow.Application.DTOs.Subscription
{
    /// <summary>
    /// Admin price row: full configuration persisted; <see cref="IsDiscountActive"/> is computed from current UTC.
    /// </summary>
    public class AdminSubscriptionPlanPriceDto
    {
        public int PriceId { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }
        /// <summary>Whether the current UTC time falls inside the discount window.</summary>
        public bool IsDiscountActive { get; set; }
        public bool IsActive { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
