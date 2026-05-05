using BizFlow.Application.Common.Constants;
using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Import
{
    /// <summary>
    /// Full detail response including items
    /// </summary>
    public class ImportDetailDto : ImportSummaryDto
    {
        public string? ImageUrl { get; set; }
        /// <summary>
        /// Payment method (optional). <c>Code</c> ∈ {<c>cash</c>, <c>bank</c>, <c>system</c>}
        /// (see <see cref="PaymentMethods"/>).
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto? PaymentMethod { get; set; }
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
