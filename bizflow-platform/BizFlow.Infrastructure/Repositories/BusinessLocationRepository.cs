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

        public async Task<IEnumerable<BusinessLocation>> GetOwnedByUserIdAsync(Guid userId)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.UserId == userId && ula.IsOwner)
                .Select(ula => ula.BusinessLocation)
                .ToListAsync();
        }

        public async Task<IEnumerable<BusinessLocation>> GetWorkLocationsByUserIdAsync(Guid userId)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.UserId == userId && !ula.IsOwner)
                .Select(ula => ula.BusinessLocation)
                .ToListAsync();
        }

        public async Task<BusinessLocation?> GetByIdAsync(int id)
        {
            return await _context.BusinessLocations.FindAsync(id);
        }

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

        public async Task<bool> IsOwnerOfLocationAsync(Guid userId, int locationId)
        {
            return await _context.UserLocationAssignments
                .AnyAsync(ula => 
                    ula.UserId == userId && 
                    ula.BusinessLocationId == locationId && 
                    ula.IsOwner);
        }

        public async Task<BusinessLocation> AddAsync(BusinessLocation location)
        {
            var entry = await _context.BusinessLocations.AddAsync(location);
            return entry.Entity;
        }

        public void Update(BusinessLocation location)
        {
            _context.BusinessLocations.Update(location);
        }

        public async Task AddUserLocationAssignmentAsync(UserLocationAssignment assignment)
        {
            await _context.UserLocationAssignments.AddAsync(assignment);
        }
    }
}
