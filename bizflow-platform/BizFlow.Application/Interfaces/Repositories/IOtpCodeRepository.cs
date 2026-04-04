using BizFlow.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IOtpCodeRepository
    {
        /// <summary>Gets the latest active OTP for the email.</summary>
        Task<OtpCode?> GetLatestActiveOtpAsync(string email, CancellationToken ct = default);

        /// <summary>Sets IsUsed = true for all active OTPs for the email.</summary>
        Task DisableAllActiveOtpsAsync(string email, CancellationToken ct = default);

        /// <summary>Deletes OTP rows that are used or expired before <paramref name="before"/>.</summary>
        Task DeleteExpiredOtpsAsync(DateTime before, CancellationToken ct = default);

        Task AddAsync(OtpCode otpCode);
        Task UpdateAsync(OtpCode otpCode);
    }
}
