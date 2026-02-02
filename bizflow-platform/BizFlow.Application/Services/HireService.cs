using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Mappers;

namespace BizFlow.Application.Services
{
    public class HireService : IHireService
    {
        private readonly IUnitOfWork _unitOfWork;

        public HireService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region Query Methods

        /// <summary>
        /// Gets basic employee summaries (id, name) for selection/dropdowns
        /// </summary>
        public async Task<EmployeeSummaryListDto> GetEmployeeSummariesAsync(Guid ownerId)
        {
            var employees = await _unitOfWork.Hires.GetHiredEmployeesWithDetailsAsync(ownerId);

            return new EmployeeSummaryListDto
            {
                Employees = employees.Select(e => new EmployeeSummaryDto
                {
                    UserId = e.hire.EmployeeId.ToString(),
                    UserName = e.fullName
                }).ToList()
            };
        }

        /// <summary>
        /// Gets all hired employees for an owner with full details (for management)
        /// </summary>
        public async Task<IEnumerable<HiredEmployeeDto>> GetHiredEmployeeDetailsAsync(Guid ownerId)
        {
            var employees = await _unitOfWork.Hires.GetHiredEmployeesWithDetailsAsync(ownerId);

            return employees.Select(e => HireMapper.ToDto(e.hire, e.fullName, e.email, e.phone));
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates which employees are hired and available for assignment
        /// </summary>
        public async Task<EmployeeValidationResult> ValidateEmployeesForAssignmentAsync(Guid ownerId, IEnumerable<Guid> employeeIds)
        {
            var result = new EmployeeValidationResult();

            // Get all hired employees for this owner
            var hiredEmployeeIds = await _unitOfWork.Hires.GetHiredEmployeeIdsAsync(ownerId);
            var hiredEmployeeIdsSet = hiredEmployeeIds.ToHashSet();

            // Separate valid from invalid employees
            var distinctEmployeeIds = employeeIds.Distinct().ToList();

            foreach (var employeeId in distinctEmployeeIds)
            {
                if (hiredEmployeeIdsSet.Contains(employeeId))
                {
                    result.ValidEmployeeIds.Add(employeeId);
                }
                else
                {
                    result.InvalidEmployeeIds.Add(employeeId);
                }
            }

            return result;
        }

        #endregion
    }
}
