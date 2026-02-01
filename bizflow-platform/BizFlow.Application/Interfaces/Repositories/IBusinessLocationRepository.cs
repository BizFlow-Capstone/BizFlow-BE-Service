using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IBusinessLocationRepository
    {
        // ============ Query Methods ============

        /// <summary>
        /// Get all locations owned by a user (is_owner = true)
        /// </summary>
        Task<IEnumerable<BusinessLocation>> GetOwnedByUserIdAsync(Guid userId);

        /// <summary>
        /// Get all locations where user works at (is_owner = false)
        /// </summary>
        Task<IEnumerable<BusinessLocation>> GetWorkLocationsByUserIdAsync(Guid userId);

        /// <summary>
        /// Get location by ID
        /// </summary>
        Task<BusinessLocation?> GetByIdAsync(int id);

        /// <summary>
        /// Get location by ID with owner info
        /// </summary>
        Task<(BusinessLocation? Location, string? OwnerName)> GetByIdWithOwnerAsync(int id);

        /// <summary>
        /// Check if user is owner of specific location
        /// </summary>
        Task<bool> IsOwnerOfLocationAsync(Guid userId, int locationId);

        /// <summary>
        /// Checks if location name already exists for an owner
        /// </summary>
        Task<bool> IsExistedByNameAsync(Guid userId, string locationName);

        /// <summary>
        /// Gets employee IDs already assigned to a location
        /// </summary>
        Task<IEnumerable<Guid>> GetAssignedEmployeeIdsAsync(int locationId);

        // ============ Command Methods ============

        /// <summary>
        /// Add a new location
        /// </summary>
        Task<BusinessLocation> AddAsync(BusinessLocation location);

        /// <summary>
        /// Update a location
        /// </summary>
        void Update(BusinessLocation location);

        /// <summary>
        /// Add user location assignment
        /// </summary>
        Task AddUserLocationAssignmentAsync(UserLocationAssignment assignment);
    }
}
