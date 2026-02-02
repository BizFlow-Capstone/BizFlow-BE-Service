using BizFlow.Application.DTOs.Location;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IBusinessLocationService
    {
        /// <summary>
        /// Get all locations owned by the user
        /// </summary>
        Task<IEnumerable<BusinessLocationDto>> GetOwnedLocationsAsync(Guid userId);

        /// <summary>
        /// Get all locations where user works at (as employee)
        /// </summary>
        Task<IEnumerable<BusinessLocationDto>> GetWorkLocationsAsync(Guid userId);

        /// <summary>
        /// Create a new location (user becomes owner)
        /// </summary>
        Task<BusinessLocationDto> CreateLocationAsync(Guid userId, CreateLocationRequest request);

        /// <summary>
        /// Update location active status (owner only)
        /// </summary>
        Task<bool> UpdateLocationStatusAsync(Guid userId, int locationId, bool isActive);

        /// <summary>
        /// Update location info (owner only)
        /// </summary>
        Task<bool> UpdateLocationAsync(Guid userId, int locationId, UpdateLocationRequest request);

        /// <summary>
        /// Add employees to a location (owner only)
        /// </summary>
        Task<bool> AddEmployeesToLocationAsync(Guid ownerId, int locationId, List<Guid> employeeIds);

        /// <summary>
        /// Delete location (soft delete) - owner only
        /// </summary>
        Task<bool> DeleteLocationAsync(Guid userId, int locationId);
    }
}
