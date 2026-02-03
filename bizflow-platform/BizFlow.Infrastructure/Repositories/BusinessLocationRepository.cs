using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class BusinessLocationRepository : IBusinessLocationRepository
    {
        private readonly BizFlowDbContext _context;

        public BusinessLocationRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        #region Query Methods

        /// <summary>
        /// Gets all locations owned by a user (IsOwner = true)
        /// </summary>
        public async Task<IEnumerable<BusinessLocation>> GetOwnedByUserIdAsync(Guid userId)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.UserId == userId && ula.IsOwner)
                .Join(_context.BusinessLocations,
                    ula => ula.BusinessLocationId,
                    loc => loc.BusinessLocationId,
                    (ula, loc) => loc)
                .ToListAsync();
        }

        /// <summary>
        /// Gets all locations where user works (IsOwner = false)
        /// </summary>
        public async Task<IEnumerable<BusinessLocation>> GetWorkLocationsByUserIdAsync(Guid userId)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.UserId == userId && !ula.IsOwner)
                .Join(_context.BusinessLocations,
                    ula => ula.BusinessLocationId,
                    loc => loc.BusinessLocationId,
                    (ula, loc) => loc)
                .ToListAsync();
        }

        /// <summary>
        /// Gets a location by ID
        /// </summary>
        public async Task<BusinessLocation?> GetByIdAsync(int id)
        {
            return await _context.BusinessLocations
                .FirstOrDefaultAsync(loc => loc.BusinessLocationId == id);
        }

        /// <summary>
        /// Gets a location by ID with owner's full name
        /// </summary>
        public async Task<(BusinessLocation? Location, string? OwnerName)> GetByIdWithOwnerAsync(int id)
        {
            var result = await (
                from loc in _context.BusinessLocations
                where loc.BusinessLocationId == id
                join ula in _context.UserLocationAssignments
                    on loc.BusinessLocationId equals ula.BusinessLocationId
                where ula.IsOwner
                join user in _context.Users
                    on ula.UserId equals user.UserId
                select new { Location = loc, OwnerName = user.FullName }
            ).FirstOrDefaultAsync();

            return result == null 
                ? (null, null) 
                : (result.Location, result.OwnerName);
        }

        /// <summary>
        /// Checks if user is owner of a location
        /// </summary>
        public async Task<bool> IsOwnerOfLocationAsync(Guid userId, int locationId)
        {
            return await _context.UserLocationAssignments
                .AnyAsync(ula => 
                    ula.UserId == userId && 
                    ula.BusinessLocationId == locationId && 
                    ula.IsOwner);
        }

        /// <summary>
        /// Checks if location name already exists for an owner
        /// </summary>
        public async Task<bool> IsExistedByNameAsync(Guid userId, string locationName)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.UserId == userId && ula.IsOwner)
                .Join(_context.BusinessLocations,
                    ula => ula.BusinessLocationId,
                    loc => loc.BusinessLocationId,
                    (ula, loc) => loc)
                .AnyAsync(loc => loc.Name == locationName);
        }

        /// <summary>
        /// Gets employee IDs already assigned to a location
        /// </summary>
        public async Task<IEnumerable<Guid>> GetAssignedEmployeeIdsAsync(int locationId)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.BusinessLocationId == locationId && !ula.IsOwner && ula.IsActive == true)
                .Select(ula => ula.UserId)
                .ToListAsync();
        }

        /// <summary>
        /// Gets basic info of employees assigned to a location
        /// </summary>
        public async Task<IEnumerable<(Guid UserId, string FullName, string Email)>> GetEmployeesByLocationIdAsync(int locationId)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.BusinessLocationId == locationId && !ula.IsOwner && ula.IsActive == true)
                .Join(_context.Users,
                    ula => ula.UserId,
                    user => user.UserId,
                    (ula, user) => new { user.UserId, user.FullName, user.Email })
                .Select(x => new ValueTuple<Guid, string, string>(x.UserId, x.FullName, x.Email))
                .ToListAsync();
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Adds a new location to database
        /// </summary>
        public async Task<BusinessLocation> AddAsync(BusinessLocation location)
        {
            var entry = await _context.BusinessLocations.AddAsync(location);
            return entry.Entity;
        }

        /// <summary>
        /// Updates an existing location
        /// </summary>
        public void Update(BusinessLocation location)
        {
            _context.BusinessLocations.Update(location);
        }

        /// <summary>
        /// Adds user-location assignment (owner or employee)
        /// </summary>
        public async Task AddUserLocationAssignmentAsync(UserLocationAssignment assignment)
        {
            await _context.UserLocationAssignments.AddAsync(assignment);
        }

        #endregion
    }
}
