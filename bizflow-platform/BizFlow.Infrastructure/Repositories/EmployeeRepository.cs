using BizFlow.Application.DTOs.Employee;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly BizFlowDbContext _context;

        public EmployeeRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserSearchResultDto>> SearchByContactAsync(Guid ownerId, string query, int limit = 10)
        {
            query = query.Trim();

            var matchedProfiles = await _context.Profiles
                .Where(profile => profile.ProfileId != ownerId)
                .Where(profile => _context.Credentials.Any(credential =>
                    credential.AccountId == profile.AccountId &&
                    (credential.Type == "phone" || credential.Type == "email") &&
                    EF.Functions.Like(credential.Identifier, $"%{query}%")))
                .Select(profile => new UserSearchResultDto
                {
                    UserId = profile.ProfileId,
                    FullName = profile.FullName,
                    AvatarUrl = profile.AvatarUrl,
                    IsAlreadyHired = _context.Hires.Any(hire =>
                        hire.OwnerId == ownerId &&
                        hire.EmployeeId == profile.ProfileId &&
                        hire.IsActive == true)
                })
                .Take(limit)
                .ToListAsync();

            return matchedProfiles;
        }

        public Task<bool> ProfileExistsAsync(Guid profileId)
        {
            return _context.Profiles.AnyAsync(profile => profile.ProfileId == profileId);
        }

        public Task<Hire?> GetHireByOwnerEmployeeAsync(Guid ownerId, Guid employeeId)
        {
            return _context.Hires.FirstOrDefaultAsync(hire =>
                hire.OwnerId == ownerId &&
                hire.EmployeeId == employeeId);
        }

        public Task<Hire?> GetOpenHireAsync(Guid ownerId, Guid employeeId)
        {
            return _context.Hires.FirstOrDefaultAsync(hire =>
                hire.OwnerId == ownerId &&
                hire.EmployeeId == employeeId &&
                (hire.Status == "pending" || hire.Status == "accepted"));
        }

        public Task<Hire?> GetActiveHireAsync(Guid ownerId, Guid employeeId)
        {
            return _context.Hires.FirstOrDefaultAsync(hire =>
                hire.OwnerId == ownerId &&
                hire.EmployeeId == employeeId &&
                hire.Status == "accepted" &&
                hire.IsActive == true);
        }

        public async Task<Hire> CreateHireAsync(Hire hire)
        {
            _context.Hires.Add(hire);
            await _context.SaveChangesAsync();
            return hire;
        }

        public async Task DeleteHireAsync(Hire hire)
        {
            _context.Hires.Remove(hire);
            await _context.SaveChangesAsync();
        }

        public Task<bool> HasActiveAssignmentsAsync(Guid employeeId)
        {
            return _context.UserLocationAssignments.AnyAsync(assignment =>
                assignment.UserId == employeeId && assignment.IsActive == true);
        }

        public async Task<List<EmployeeInvitationDto>> GetPendingInvitationsAsync(Guid employeeId)
        {
            return await _context.Hires
                .Where(hire =>
                    hire.EmployeeId == employeeId &&
                    hire.Status == "pending")
                .Join(
                    _context.Profiles,
                    hire => hire.OwnerId,
                    profile => profile.ProfileId,
                    (hire, profile) => new EmployeeInvitationDto
                    {
                        HireId = hire.HireId,
                        OwnerId = hire.OwnerId,
                        OwnerName = profile.FullName,
                        InvitedAt = hire.StartAt
                    })
                .OrderByDescending(invitation => invitation.InvitedAt)
                .ToListAsync();
        }

        public Task<Hire?> GetPendingInvitationByIdAsync(Guid employeeId, int hireId)
        {
            return _context.Hires.FirstOrDefaultAsync(hire =>
                hire.HireId == hireId &&
                hire.EmployeeId == employeeId &&
                hire.Status == "pending");
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
