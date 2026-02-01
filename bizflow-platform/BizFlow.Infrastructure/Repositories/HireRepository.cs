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
                .Where(h => h.OwnerId == ownerId && h.IsActive)
                .Select(h => h.EmployeeId)
                .ToListAsync();
        }

        /// <summary>
        /// Gets hired employees with user details (name, email, phone)
        /// </summary>
        public async Task<IEnumerable<(Hire hire, string fullName, string email, string? phone)>> GetHiredEmployeesWithDetailsAsync(Guid ownerId)
        {
            var result = await _context.Hires
                .Where(h => h.OwnerId == ownerId)
                .Join(_context.Users,
                    hire => hire.EmployeeId,
                    user => user.UserId,
                    (hire, user) => new { hire, user })
                .Select(x => new
                {
                    Hire = x.hire,
                    x.user.FullName,
                    x.user.Email,
                    x.user.Phone
                })
                .ToListAsync();

            return result.Select(x => (x.Hire, x.FullName, x.Email, x.Phone));
        }

        #endregion
    }
}
