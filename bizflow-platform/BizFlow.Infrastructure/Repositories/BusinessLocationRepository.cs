using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;
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
        /// Single query: locations + owner name. Replaces N+1 pattern.
        /// </summary>
        public async Task<IEnumerable<BusinessLocationDto>> GetLocationsByUserAsync(Guid userId, bool isOwner)
        {
            return await (
                from ula in _context.UserLocationAssignments
                where ula.UserId == userId && ula.IsOwner == isOwner && ula.IsActive == true
                join loc in _context.BusinessLocations
                    on ula.BusinessLocationId equals loc.BusinessLocationId
                where loc.DeletedAt == null
                      && (isOwner || loc.IsActive == true) // RULE-LOC-07: Employee only sees active locations
                join ownerUla in _context.UserLocationAssignments
                    on new { loc.BusinessLocationId, IsOwner = true }
                    equals new { ownerUla.BusinessLocationId, ownerUla.IsOwner }
                join ownerProfile in _context.Profiles
                    on ownerUla.UserId equals ownerProfile.ProfileId
                select new BusinessLocationDto
                {
                    Id = loc.BusinessLocationId,
                    Name = loc.LocationName,
                    Address = loc.Address,
                    District = loc.District,
                    City = loc.City,
                    Phone = loc.Phone,
                    IsActive = loc.IsActive ?? false,
                    OwnerProfileId = ownerProfile.ProfileId,
                    OwnerName = ownerProfile.FullName
                }
            ).ToListAsync();
        }

        public async Task<BusinessLocation?> GetByIdAsync(int id)
        {
            return await _context.BusinessLocations
                .FirstOrDefaultAsync(loc => loc.BusinessLocationId == id && loc.DeletedAt == null);
        }

        public async Task<BusinessLocationDto?> GetLocationDtoByUserAndIdAsync(Guid userId, int locationId)
        {
            return await (
                from ula in _context.UserLocationAssignments
                where ula.UserId == userId && ula.IsOwner && ula.IsActive == true
                join loc in _context.BusinessLocations
                    on ula.BusinessLocationId equals loc.BusinessLocationId
                where loc.BusinessLocationId == locationId && loc.DeletedAt == null
                join ownerUla in _context.UserLocationAssignments
                    on new { loc.BusinessLocationId, IsOwner = true }
                    equals new { ownerUla.BusinessLocationId, ownerUla.IsOwner }
                join ownerProfile in _context.Profiles
                    on ownerUla.UserId equals ownerProfile.ProfileId
                select new BusinessLocationDto
                {
                    Id = loc.BusinessLocationId,
                    Name = loc.LocationName,
                    Address = loc.Address,
                    District = loc.District,
                    City = loc.City,
                    Phone = loc.Phone,
                    IsActive = loc.IsActive ?? false,
                    OwnerProfileId = ownerProfile.ProfileId,
                    OwnerName = ownerProfile.FullName
                }
            ).FirstOrDefaultAsync();
        }

        public async Task<BusinessLocationDetailDto?> GetLocationDetailByIdAsync(int locationId)
        {
            var detail = await (
                from loc in _context.BusinessLocations
                where loc.BusinessLocationId == locationId && loc.DeletedAt == null
                join ownerUla in _context.UserLocationAssignments
                    on new { loc.BusinessLocationId, IsOwner = true }
                    equals new { ownerUla.BusinessLocationId, ownerUla.IsOwner }
                join ownerProfile in _context.Profiles
                    on ownerUla.UserId equals ownerProfile.ProfileId
                select new BusinessLocationDetailDto
                {
                    Id = loc.BusinessLocationId,
                    Name = loc.LocationName,
                    Address = loc.Address,
                    District = loc.District,
                    City = loc.City,
                    Phone = loc.Phone,
                    TaxCode = loc.TaxCode,
                    IsActive = loc.IsActive ?? false,
                    OwnerProfileId = ownerProfile.ProfileId,
                    OwnerName = ownerProfile.FullName
                }
            ).FirstOrDefaultAsync();

            if (detail == null) return null;

            detail.Employees = await (
                from ula in _context.UserLocationAssignments
                where ula.BusinessLocationId == locationId && !ula.IsOwner && ula.IsActive == true
                join profile in _context.Profiles
                    on ula.UserId equals profile.ProfileId
                join account in _context.Accounts
                    on profile.AccountId equals account.AccountId
                select new EmployeeSummaryDto
                {
                    ProfileId = ula.UserId.ToString(),
                    UserName = profile.FullName,
                    Phone = account.Credentials.Where(c => c.AccountId == account.AccountId && c.Type == "phone").Select(c => c.Identifier).FirstOrDefault() ?? string.Empty,
                }
            ).ToListAsync();

            return detail;
        }

        public async Task<bool> IsOwnerOfLocationAsync(Guid userId, int locationId)
        {
            return await _context.UserLocationAssignments
                .AnyAsync(ula =>
                    ula.UserId == userId &&
                    ula.BusinessLocationId == locationId &&
                    ula.IsOwner);
        }

        public async Task<bool> HasAccessToLocationAsync(Guid userId, int locationId)
        {
            return await _context.UserLocationAssignments
                .AnyAsync(ula =>
                    ula.UserId == userId &&
                    ula.BusinessLocationId == locationId &&
                    ula.IsActive == true);
        }

        public async Task<Guid?> GetOwnerIdByLocationAsync(int locationId)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.BusinessLocationId == locationId && ula.IsOwner && ula.IsActive == true)
                .Select(ula => (Guid?)ula.UserId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsExistedByNameAsync(Guid userId, string locationName)
        {
            return await (
                from ula in _context.UserLocationAssignments
                where ula.UserId == userId && ula.IsOwner
                join loc in _context.BusinessLocations
                    on ula.BusinessLocationId equals loc.BusinessLocationId
                where loc.DeletedAt == null && loc.LocationName == locationName
                select loc
            ).AnyAsync();
        }

        public async Task<IEnumerable<Guid>> GetAssignedEmployeeIdsAsync(int locationId)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.BusinessLocationId == locationId && !ula.IsOwner && ula.IsActive == true)
                .Select(ula => ula.UserId)
                .ToListAsync();
        }

        public async Task<bool> HasAnyActiveAssignmentWithOwnerAsync(Guid ownerId, Guid employeeId)
        {
            return await (
                from employeeAssignment in _context.UserLocationAssignments
                where employeeAssignment.UserId == employeeId
                      && !employeeAssignment.IsOwner
                      && employeeAssignment.IsActive == true
                join ownerAssignment in _context.UserLocationAssignments
                    on employeeAssignment.BusinessLocationId equals ownerAssignment.BusinessLocationId
                where ownerAssignment.UserId == ownerId
                      && ownerAssignment.IsOwner
                      && ownerAssignment.IsActive == true
                select employeeAssignment.UserLocationAssignmentId
            ).AnyAsync();
        }

        public async Task<IEnumerable<(Guid UserId, string FullName, string Email, string? Phone)>> GetEmployeesByLocationIdAsync(int locationId)
        {
            return await _context.UserLocationAssignments
                .Where(ula => ula.BusinessLocationId == locationId && !ula.IsOwner && ula.IsActive == true)
                .Join(_context.Profiles,
                    ula => ula.UserId,
                    profile => profile.ProfileId,
                    (ula, profile) => new { ula, profile })
                .Join(_context.Accounts,
                    x => x.profile.AccountId,
                    account => account.AccountId,
                    (x, account) => new
                    {
                        x.profile.ProfileId,
                        x.profile.FullName,
                        Email = _context.Credentials
                            .Where(c => c.AccountId == account.AccountId && c.Type == "email")
                            .Select(c => c.Identifier).FirstOrDefault() ?? string.Empty,
                        Phone = _context.Credentials
                            .Where(c => c.AccountId == account.AccountId && c.Type == "phone")
                            .Select(c => c.Identifier).FirstOrDefault()
                    })
                .Select(x => new ValueTuple<Guid, string, string, string?>(x.ProfileId, x.FullName, x.Email, x.Phone))
                .ToListAsync();
        }

        #endregion

        #region Command Methods

        public async Task<bool> HasRelatedDataAsync(int locationId)
        {
            var hasProducts = await _context.Products
                .AnyAsync(p => p.BusinessLocationId == locationId);
            if (hasProducts) return true;

            var hasImports = await _context.Imports
                .AnyAsync(i => i.BusinessLocationId == locationId);
            if (hasImports) return true;

            var hasEmployees = await _context.UserLocationAssignments
                .AnyAsync(ula => ula.BusinessLocationId == locationId && !ula.IsOwner);
            return hasEmployees;
        }

        public async Task<List<int>> GetAllActiveLocationIdsAsync()
        {
            return await _context.BusinessLocations
                .Where(l => l.DeletedAt == null && l.IsActive == true)
                .Select(l => l.BusinessLocationId)
                .ToListAsync();
        }

        #endregion

        #region Command Methods

        public async Task<BusinessLocation> AddAsync(BusinessLocation location)
        {
            var entry = await _context.BusinessLocations.AddAsync(location);
            return entry.Entity;
        }

        public void Update(BusinessLocation location)
        {
            _context.BusinessLocations.Update(location);
        }

        public void Delete(BusinessLocation location)
        {
            _context.BusinessLocations.Remove(location);
        }

        public async Task AddUserLocationAssignmentAsync(UserLocationAssignment assignment)
        {
            await _context.UserLocationAssignments.AddAsync(assignment);
        }

        public async Task RemoveEmployeeFromLocationAsync(int locationId, Guid employeeId)
        {
            var assignment = await _context.UserLocationAssignments
                .Where(ula =>
                    ula.BusinessLocationId == locationId &&
                    ula.UserId == employeeId &&
                    !ula.IsOwner &&
                    ula.IsActive == true)
                .OrderByDescending(ula => ula.AssignedAt)
                .ThenByDescending(ula => ula.UserLocationAssignmentId)
                .FirstOrDefaultAsync();

            if (assignment != null)
            {
                assignment.IsActive = false;
                assignment.UnassignedAt = DateTime.UtcNow;
            }
        }

        #endregion
    }
}
