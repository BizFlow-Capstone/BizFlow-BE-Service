namespace BizFlow.Application.DTOs.Subscription
{
    /// <summary>
    /// Raw price data for admin view — no computed fields.
    /// </summary>
    public class AdminSubscriptionPlanPriceDto
    {
        public int PriceId { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }
        public bool IsActive { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
