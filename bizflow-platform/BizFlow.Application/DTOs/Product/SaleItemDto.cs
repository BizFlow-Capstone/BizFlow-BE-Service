namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Sale item (price tier) DTO
    /// </summary>
    public class SaleItemDto
    {
        public long SaleItemId { get; set; }
        public string Unit { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
