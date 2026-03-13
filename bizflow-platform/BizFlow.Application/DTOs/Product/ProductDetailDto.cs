namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Detailed product information response
    /// </summary>
    public class ProductDetailDto
    {
        public long ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? Sku { get; set; }
        public string? ImageUrl { get; set; }
        public string Unit { get; set; } = null!;
        public decimal SellingPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int Stock { get; set; }
        public string? Manufacturer { get; set; }
        public string Status { get; set; } = null!;
        public bool TrackInventory { get; set; }
        public int BusinessLocationId { get; set; }
        public string BusinessLocationName { get; set; } = null!;
        public List<SaleItemDto> SaleItems { get; set; } = new List<SaleItemDto>();
    }
}
