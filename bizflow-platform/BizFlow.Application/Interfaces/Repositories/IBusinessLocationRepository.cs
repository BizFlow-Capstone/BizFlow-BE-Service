using BizFlow.Application.DTOs.Location;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IBusinessLocationRepository
    {
        // ============ Query Methods ============

        /// <summary>
        /// Get locations by user with owner name — eliminates N+1.
        /// isOwner = true  → owned locations
        /// isOwner = false → work (employee) locations
        /// </summary>
        Task<IEnumerable<BusinessLocationDto>> GetLocationsByUserAsync(Guid userId, bool isOwner);

        /// <summary>
        /// Get location by ID (non-deleted only)
        /// </summary>
        Task<BusinessLocation?> GetByIdAsync(int id);

        /// <summary>
        /// Get location detail with owner name and employee count (non-deleted only)
        /// </summary>
        Task<BusinessLocationDetailDto?> GetLocationDetailByIdAsync(int locationId);

        /// <summary>
        /// Check if user is owner of specific location
        /// </summary>
        Task<bool> IsOwnerOfLocationAsync(Guid userId, int locationId);

        /// <summary>
        /// Check if user has access to location (owner or assigned employee)
        /// </summary>
        Task<bool> HasAccessToLocationAsync(Guid userId, int locationId);

        /// <summary>
        /// Checks if location name already exists for an owner
        /// </summary>
        Task<bool> IsExistedByNameAsync(Guid userId, string locationName);

        /// <summary>
        /// Gets employee IDs already assigned to a location
        /// </summary>
        Task<IEnumerable<Guid>> GetAssignedEmployeeIdsAsync(int locationId);

        /// <summary>
        /// Gets basic info of employees assigned to a location
        /// </summary>
        Task<IEnumerable<(Guid UserId, string FullName, string Email, string? Phone)>> GetEmployeesByLocationIdAsync(int locationId);

        /// <summary>
        /// Check if location has any related data (products, imports, employee assignments)
        /// </summary>
        Task<bool> HasRelatedDataAsync(int locationId);

        // ============ Command Methods ============

        Task<BusinessLocation> AddAsync(BusinessLocation location);
        void Update(BusinessLocation location);
        void Delete(BusinessLocation location);
        Task AddUserLocationAssignmentAsync(UserLocationAssignment assignment);

        /// <summary>
        /// Deactivate employee assignment from location
        /// </summary>
        Task RemoveEmployeeFromLocationAsync(int locationId, Guid employeeId);
    }
}
