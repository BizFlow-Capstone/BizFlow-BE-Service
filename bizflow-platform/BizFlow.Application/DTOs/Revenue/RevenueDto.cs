using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Revenue
{
    public class RevenueDto
    {
        public long RevenueId { get; set; }
        public int BusinessLocationId { get; set; }
        public Guid BusinessTypeId { get; set; }
        public string? BusinessTypeName { get; set; }
        public long? OrderId { get; set; }
        /// <summary>
        /// Revenue type. <c>Code</c> ∈ {<c>sale</c>, <c>manual</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto RevenueType { get; set; } = null!;
        /// <summary>
        /// Revenue lifecycle status. <c>Code</c> ∈ {<c>draft</c>, <c>posted</c>, <c>cancelled</c>, <c>replaced</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateOnly RevenueDate { get; set; }
        public string Description { get; set; } = null!;
        /// <summary>
        /// Money channel (optional). <c>Code</c> ∈ {<c>cash</c>, <c>bank</c>, <c>debt</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto? MoneyChannel { get; set; }
        public string? DocumentUrl { get; set; }
        public string? DocumentPublicId { get; set; }
        public string? DocumentNumber { get; set; }
        public DateOnly? DocumentDate { get; set; }
        /// <summary>
        /// Original Revenue this record replaces (replace-when-posted flow). Null when this is a fresh record.
        /// </summary>
        public long? RefRevenueId { get; set; }

        /// <summary>True when this row is an append-only reversal (Amount is negative of the original).</summary>
        public bool IsReversal { get; set; }

        /// <summary>Original <see cref="RevenueId"/> reversed by this row, when <see cref="IsReversal"/>.</summary>
        public long? ReversedRevenueId { get; set; }

        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
