namespace BizFlow.Application.DTOs.Hire
{
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
