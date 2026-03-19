using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class HireRepository : IHireRepository
    {
        private readonly BizFlowDbContext _context;

        public HireRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        #region Query Methods

        /// <summary>
        /// Gets active hired employee IDs for an owner
        /// </summary>
        public async Task<IEnumerable<Guid>> GetHiredEmployeeIdsAsync(Guid ownerId)
        {
            return await _context.Hires
                .Where(h => h.OwnerId == ownerId && h.IsActive == true)
                .Select(h => h.EmployeeId)
                .ToListAsync();
        }

        /// <summary>
        /// Gets hired employees with user details (name, email, phone)
        /// </summary>
        public async Task<IEnumerable<(Hire hire, string fullName, string email, string? phone)>> GetHiredEmployeesWithDetailsAsync(Guid ownerId)
        {
            var result = await _context.Hires
                .Where(h => h.OwnerId == ownerId && h.Status != "rejected")
                .Join(_context.Profiles,
                    hire => hire.EmployeeId,
                    profile => profile.ProfileId,
                    (hire, profile) => new { hire, profile })
                .Join(_context.Accounts,
                    x => x.profile.AccountId,
                    account => account.AccountId,
                    (x, account) => new { x.hire, x.profile, account })
                .Select(x => new
                {
                    Hire = x.hire,
                    x.profile.FullName,
                    Email = _context.Credentials
                        .Where(c => c.AccountId == x.account.AccountId && c.Type == "email")
                        .Select(c => c.Identifier).FirstOrDefault() ?? string.Empty,
                    Phone = _context.Credentials
                        .Where(c => c.AccountId == x.account.AccountId && c.Type == "phone")
                        .Select(c => (string?)c.Identifier).FirstOrDefault()
                })
                .ToListAsync();

            return result.Select(x => (x.Hire, x.FullName, x.Email, x.Phone));
        }

        #endregion
    }
}
