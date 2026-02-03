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
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? Phone { get; set; }
    }
}
