using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Mappers;
using AutoMapper;

namespace BizFlow.Application.Services
{
    public class HireService : IHireService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public HireService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
                Employees = _mapper.Map<List<EmployeeSummaryDto>>(employees)
            };
        }

        /// <summary>
        /// Gets all hired employees for an owner with full details (for management)
        /// </summary>
        public async Task<IEnumerable<HiredEmployeeDto>> GetHiredEmployeeDetailsAsync(Guid ownerId)
        {
            var employees = await _unitOfWork.Hires.GetHiredEmployeesWithDetailsAsync(ownerId);

            return _mapper.Map<IEnumerable<HiredEmployeeDto>>(employees);
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
