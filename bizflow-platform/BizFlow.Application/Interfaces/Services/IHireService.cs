using BizFlow.Application.DTOs.Hire;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IHireService
    {
        /// <summary>
        /// Get basic employee summaries (id, name only) for selection/dropdowns
        /// </summary>
        Task<EmployeeSummaryListDto> GetEmployeeSummariesAsync(Guid ownerId);

        /// <summary>
        /// Get all employees hired by the owner with full details (for management)
        /// </summary>
        Task<IEnumerable<HiredEmployeeDto>> GetHiredEmployeeDetailsAsync(Guid ownerId);

        /// <summary>
        /// Validate which employees are hired by owner and available for assignment
        /// </summary>
        Task<EmployeeValidationResult> ValidateEmployeesForAssignmentAsync(Guid ownerId, IEnumerable<Guid> employeeIds);
    }
}
