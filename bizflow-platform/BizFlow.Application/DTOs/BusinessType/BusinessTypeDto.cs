using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.BusinessType
{
    public class BusinessTypeDto
    {
        public Guid BusinessTypeId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        /// <summary>
        /// Business type status. <c>Code</c> ∈ {<c>active</c>, <c>inactive</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
    }
}
