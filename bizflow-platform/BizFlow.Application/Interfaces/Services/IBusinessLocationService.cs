using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IBusinessLocationService
    {
        // Query
        Task<IEnumerable<BusinessLocationDto>> GetOwnedLocationsAsync(Guid userId);
        Task<IEnumerable<BusinessLocationDto>> GetWorkLocationsAsync(Guid userId);
        Task<BusinessLocationDetailDto> GetLocationDetailAsync(Guid userId, int locationId);
        Task<EmployeeSummaryListDto> GetEmployeesByLocationAsync(Guid userId, int locationId);

        // Reusable guard — validates access + blocks Employee when IsActive=false (RULE-LOC-07)
        Task ValidateLocationAccessAsync(Guid userId, int locationId);

        // Reusable guard — owner-only operations; throws ForbiddenException if not owner
        Task ValidateOwnerAsync(Guid userId, int locationId);

        // Command — all throw exceptions on failure (no bool return)
        Task<BusinessLocationDto> CreateLocationAsync(Guid userId, CreateLocationRequest request);
        Task UpdateLocationAsync(Guid userId, int locationId, UpdateLocationRequest request);
        Task UpdateLocationStatusAsync(Guid userId, int locationId, bool isActive);
        Task AddEmployeesToLocationAsync(Guid ownerId, int locationId, List<Guid> employeeIds);
        Task RemoveEmployeeFromLocationAsync(Guid ownerId, int locationId, Guid employeeId);
        Task DeleteLocationAsync(Guid userId, int locationId);
    }
}
