namespace BizFlow.Application.DTOs.Hire
{
    /// <summary>
    /// DTO for hired employee information
    /// </summary>
    public class HiredEmployeeDto
    {
        public Guid EmployeeId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }

    /// <summary>
    /// Result of employee validation for assignment
    /// </summary>
    public class EmployeeValidationResult
    {
        public List<Guid> ValidEmployeeIds { get; set; } = new();
        public List<Guid> InvalidEmployeeIds { get; set; } = new();
        public bool AllValid => InvalidEmployeeIds.Count == 0;
    }
}
