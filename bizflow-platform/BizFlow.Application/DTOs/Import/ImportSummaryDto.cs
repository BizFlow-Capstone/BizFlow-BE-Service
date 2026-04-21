using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Import
{
    /// <summary>
    /// Used in list and create/update responses
    /// </summary>
    public class ImportSummaryDto
    {
        public long ImportId { get; set; }
        public string? ImportCode { get; set; }
        /// <summary>
        /// Import type. <c>Code</c> ∈ {<c>INVOICE</c>, <c>INVENTORY_ADJUSTMENT</c>, <c>RETURN</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto ImportType { get; set; } = null!;
        /// <summary>
        /// Import status. <c>Code</c> ∈ {<c>DRAFT</c>, <c>CONFIRMED</c>, <c>CANCELLED</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
        public int BusinessLocationId { get; set; }
        public string? BusinessLocationName { get; set; }
        public string? Supplier { get; set; }
        public string? Note { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
