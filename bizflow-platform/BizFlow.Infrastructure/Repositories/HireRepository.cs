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

        public async Task<bool> IsEmployeeHiredByOwnerAsync(Guid ownerId, Guid employeeId)
        {
            return await _context.Hires
                .AnyAsync(h => h.OwnerId == ownerId 
                    && h.EmployeeId == employeeId 
                    && h.IsActive);
        }

        public async Task<IEnumerable<Guid>> GetHiredEmployeeIdsAsync(Guid ownerId)
        {
            return await _context.Hires
                .Where(h => h.OwnerId == ownerId && h.IsActive)
                .Select(h => h.EmployeeId)
                .ToListAsync();
        }

        public async Task<Hire?> GetHireAsync(Guid ownerId, Guid employeeId)
        {
            return await _context.Hires
                .FirstOrDefaultAsync(h => h.OwnerId == ownerId && h.EmployeeId == employeeId);
        }

        public async Task<Hire> AddAsync(Hire hire)
        {
            var entry = await _context.Hires.AddAsync(hire);
            return entry.Entity;
        }

        public void Update(Hire hire)
        {
            _context.Hires.Update(hire);
        }

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
                    FullName = x.user.FullName,
                    Email = x.user.Email,
                    Phone = x.user.Phone
                })
                .ToListAsync();

            return result.Select(x => (x.Hire, x.FullName, x.Email, x.Phone));
        }
    }
}
