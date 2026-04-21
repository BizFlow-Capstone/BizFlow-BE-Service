using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Import
{
    /// <summary>
    /// Response for PATCH confirm/cancel
    /// </summary>
    public class ImportPatchResultDto
    {
        public long ImportId { get; set; }
        public string? ImportCode { get; set; }
        /// <summary>
        /// Import status after patch. <c>Code</c> ∈ {<c>DRAFT</c>, <c>CONFIRMED</c>, <c>CANCELLED</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
        public DateTime? ReceivedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
