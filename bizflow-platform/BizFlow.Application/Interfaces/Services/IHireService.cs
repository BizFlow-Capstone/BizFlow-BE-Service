using BizFlow.Application.DTOs.Hire;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IHireService
    {
        /// <summary>
        /// Get all employees hired by the owner
        /// </summary>
        Task<IEnumerable<HiredEmployeeDto>> GetHiredEmployeesAsync(Guid ownerId);

        /// <summary>
        /// Validate which employees are hired by owner and available for assignment
        /// </summary>
        Task<EmployeeValidationResult> ValidateEmployeesForAssignmentAsync(Guid ownerId, IEnumerable<Guid> employeeIds);
    }
}
