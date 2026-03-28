namespace BizFlow.Application.DTOs.Subscription
{
    /// <summary>
    /// Giá cho admin: lưu đủ cấu hình; <see cref="IsDiscountActive"/> tính theo UTC hiện tại.
    /// </summary>
    public class AdminSubscriptionPlanPriceDto
    {
        public int PriceId { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }
        /// <summary>Có đang trong cửa sổ giảm giá (theo UTC) hay không.</summary>
        public bool IsDiscountActive { get; set; }
        public bool IsActive { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
