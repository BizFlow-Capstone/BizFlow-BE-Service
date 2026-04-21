using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Hire
{
    /// <summary>
    /// DTO for hired employee detailed information (for management)
    /// </summary>
    public class HiredEmployeeDto
    {
        public Guid EmployeeId { get; set; }
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        /// <summary>
        /// Hire status. <c>Code</c> ∈ {<c>pending</c>, <c>accepted</c>, <c>rejected</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
    }
}
