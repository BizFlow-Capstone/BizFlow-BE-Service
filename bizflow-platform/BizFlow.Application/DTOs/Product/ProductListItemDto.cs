namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Product item in list response
    /// </summary>
    public class ProductListItemDto
    {
        public long ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? Sku { get; set; }
        public decimal Price { get; set; }
        public bool TrackInventory { get; set; }
        public int? Stock { get; set; }
        public string Status { get; set; } = null!;
    }
}
