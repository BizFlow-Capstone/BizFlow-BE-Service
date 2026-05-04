using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Cost
{
    public class CostDto
    {
        public long CostId { get; set; }
        public int BusinessLocationId { get; set; }
        public Guid? BusinessTypeId { get; set; }
        public long? ImportId { get; set; }
        /// <summary>
        /// Cost type. <c>Code</c> ∈ {<c>import</c>, <c>salary</c>, <c>rent</c>, <c>utilities</c>,
        /// <c>transport</c>, <c>marketing</c>, <c>maintenance</c>, <c>other</c>, <c>manual</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto CostType { get; set; } = null!;
        /// <summary>
        /// Cost lifecycle status. <c>Code</c> ∈ {<c>draft</c>, <c>posted</c>, <c>cancelled</c>, <c>replaced</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateOnly CostDate { get; set; }
        /// <summary>
        /// Payment method (optional). <c>Code</c> ∈ {<c>cash</c>, <c>bank</c>, <c>system</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto? PaymentMethod { get; set; }
        public string? DocumentUrl { get; set; }
        public string? DocumentPublicId { get; set; }
        public string? DocumentNumber { get; set; }
        public DateOnly? DocumentDate { get; set; }
        /// <summary>
        /// Original Cost this record replaces (replace-when-posted flow). Null when this is a fresh record.
        /// </summary>
        public long? RefCostId { get; set; }

        /// <summary>True when this row is an append-only reversal (Amount is negative of the original).</summary>
        public bool IsReversal { get; set; }

        /// <summary>Original <see cref="CostId"/> reversed by this row, when <see cref="IsReversal"/>.</summary>
        public long? ReversedCostId { get; set; }

        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
