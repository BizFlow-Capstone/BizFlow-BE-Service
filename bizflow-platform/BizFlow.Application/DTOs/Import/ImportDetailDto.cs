namespace BizFlow.Application.DTOs.Import
{
    /// <summary>
    /// Full detail response including items
    /// </summary>
    public class ImportDetailDto : ImportSummaryDto
    {
        public string? ImageUrl { get; set; }
        public List<ImportItemDetailDto> Items { get; set; } = new();
    }

    public class ImportItemDetailDto
    {
        public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Sku { get; set; }
        public decimal Quantity { get; set; }
        public string? BaseUnit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal? CurrentStock { get; set; }
    }
}
