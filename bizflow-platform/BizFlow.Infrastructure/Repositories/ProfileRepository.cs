using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly BizFlowDbContext _context;

        public ProfileRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public Task<Profile?> GetByIdAsync(Guid profileId)
        {
            return _context.Profiles.FirstOrDefaultAsync(p => p.ProfileId == profileId);
        }

        public Task<Profile?> GetByAccountIdAsync(Guid accountId)
        {
            return _context.Profiles.FirstOrDefaultAsync(p => p.AccountId == accountId);
        }
    }
}
