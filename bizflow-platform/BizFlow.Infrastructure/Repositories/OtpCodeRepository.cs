using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BizFlow.Infrastructure.Repositories
{
    public class OtpCodeRepository : IOtpCodeRepository
    {
        private readonly BizFlowDbContext _dbContext;

        public OtpCodeRepository(BizFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<OtpCode?> GetLatestActiveOtpAsync(string email, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            return _dbContext.OtpCodes
                .Where(x => x.Email == email && !x.IsUsed && x.ExpiredAt > now)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> TryConsumeActiveOtpAsync(string email, string code, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var active = await _dbContext.OtpCodes
                .Where(x => x.Email == email && !x.IsUsed && x.ExpiredAt > now)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (active == null || active.Code != code)
                return false;

            active.IsUsed = true;
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task DisableAllActiveOtpsAsync(string email, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var active = await _dbContext.OtpCodes
                .Where(x => x.Email == email && !x.IsUsed && x.ExpiredAt > now)
                .ToListAsync(ct);

            if (active.Any())
            {
                foreach (var otp in active)
                    otp.IsUsed = true;
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        public async Task DeleteExpiredOtpsAsync(DateTime before, CancellationToken ct = default)
        {
            var stale = await _dbContext.OtpCodes
                .Where(x => x.IsUsed || x.ExpiredAt < before)
                .ToListAsync(ct);

            if (stale.Any())
            {
                _dbContext.OtpCodes.RemoveRange(stale);
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        public async Task AddAsync(OtpCode otpCode)
        {
            await _dbContext.OtpCodes.AddAsync(otpCode);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(OtpCode otpCode)
        {
            _dbContext.OtpCodes.Update(otpCode);
            await _dbContext.SaveChangesAsync();
        }
    }
}
