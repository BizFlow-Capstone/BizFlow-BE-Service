namespace BizFlow.Application.DTOs.Subscription
{
    public class SubscriptionPlanPriceDto
    {
        public int PriceId { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public decimal EffectivePrice { get; set; }
        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }
        public bool IsDiscountActive { get; set; }
        public string Currency { get; set; } = "VND";
    }
}
