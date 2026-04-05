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

        public Task<List<Profile>> GetByIdsAsync(IEnumerable<Guid> profileIds)
        {
            var ids = profileIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return Task.FromResult(new List<Profile>());

            return _context.Profiles
                .Where(p => ids.Contains(p.ProfileId))
                .ToListAsync();
        }

        public Task<string?> GetFullNameForEligibleForgotPasswordEmailAsync(string normalizedEmail, CancellationToken ct = default)
        {
            return _context.Credentials
                .AsNoTracking()
                .Where(
                    c => c.Type == "email"
                        && c.Account.DeletedAt == null
                        && c.Account.IsActive != false
                        && c.Account.Profile != null
                        && c.Identifier.ToLower() == normalizedEmail)
                .Select(c => c.Account.Profile!.FullName)
                .FirstOrDefaultAsync(ct);
        }
    }
}
