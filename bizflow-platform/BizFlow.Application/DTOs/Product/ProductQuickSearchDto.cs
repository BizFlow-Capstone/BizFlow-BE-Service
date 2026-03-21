namespace BizFlow.Application.DTOs.Product
{
    public class ProductQuickSearchDto
    {
        public long ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? Sku { get; set; }
        public string? ImageUrl { get; set; }
        public decimal SellingPrice { get; set; }
        public List<SaleItemDto> SaleItems { get; set; } = new();
    }
}