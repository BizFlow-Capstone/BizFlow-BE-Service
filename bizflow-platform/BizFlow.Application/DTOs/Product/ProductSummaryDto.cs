using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Product item in list response
    /// </summary>
    public class ProductSummaryDto
    {
        public long ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? Sku { get; set; }
        public string? ImageUrl { get; set; }
        public Guid BusinessTypeId { get; set; }
        public string BusinessTypeName { get; set; } = null!;
        public decimal Price { get; set; }
        public bool TrackInventory { get; set; }
        public decimal? Stock { get; set; }
        /// <summary>
        /// Product status. <c>Code</c> ∈ {<c>active</c>, <c>inactive</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
    }
}
