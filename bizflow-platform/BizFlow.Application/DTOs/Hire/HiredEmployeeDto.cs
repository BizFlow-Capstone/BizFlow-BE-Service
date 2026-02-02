namespace BizFlow.Application.DTOs.Hire
{
    /// <summary>
    /// DTO for hired employee detailed information (for management)
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
}
