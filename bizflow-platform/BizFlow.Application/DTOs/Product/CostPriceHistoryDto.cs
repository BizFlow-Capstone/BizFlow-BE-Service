namespace BizFlow.Application.DTOs.Product
{
    public class CostPriceHistoryDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal CurrentCostPrice { get; set; }
        public List<CostPriceHistoryItemDto> History { get; set; } = new();
    }

    public class CostPriceHistoryItemDto
    {
        public long ImportId { get; set; }
        public string? ImportCode { get; set; }
        public decimal CostPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Supplier { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
