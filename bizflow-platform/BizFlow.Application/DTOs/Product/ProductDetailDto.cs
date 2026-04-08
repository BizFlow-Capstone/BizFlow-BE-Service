namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Detailed product information response
    /// </summary>
    public class ProductDetailDto : ProductSummaryDto
    {
        public string Unit { get; set; } = null!;
        public decimal SellingPrice { get; set; }
        public decimal CostPrice { get; set; }
        public string? Manufacturer { get; set; }
        public int BusinessLocationId { get; set; }
        public string BusinessLocationName { get; set; } = null!;
        public List<SaleItemDto> SaleItems { get; set; } = new List<SaleItemDto>();
    }
}
