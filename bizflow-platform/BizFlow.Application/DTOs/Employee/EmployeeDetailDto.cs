namespace BizFlow.Application.DTOs.Employee
{
    public class EmployeeDetailDto
    {
        public Guid EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
}
