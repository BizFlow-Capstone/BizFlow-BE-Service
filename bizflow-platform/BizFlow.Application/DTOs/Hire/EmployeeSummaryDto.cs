namespace BizFlow.Application.DTOs.Hire
{
    /// <summary>
    /// Response DTO containing a list of employee summaries
    /// </summary>
    public class EmployeeSummaryListDto
    {
        public List<EmployeeSummaryDto> Employees { get; set; } = new();
    }

    /// <summary>
    /// Basic employee information with userId and userName
    /// </summary>
    public class EmployeeSummaryDto
    {
        public string ProfileId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? Phone { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsAlreadyHired { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
}
