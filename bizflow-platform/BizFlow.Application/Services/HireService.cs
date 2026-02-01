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

        public async Task<IEnumerable<HiredEmployeeDto>> GetHiredEmployeesAsync(Guid ownerId)
        {
            var employees = await _unitOfWork.Hires.GetHiredEmployeesWithDetailsAsync(ownerId);

            return employees.Select(e => HireMapper.ToDto(e.hire, e.fullName, e.email, e.phone));
        }

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
    }
}
