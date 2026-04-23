namespace BizFlow.Application.DTOs.Order
{
    public class OrderDetailDto
    {
        public long OrderDetailId { get; set; }
        public long SaleItemId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Amount { get; set; }
    }
}
