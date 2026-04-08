using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IProfileRepository
    {
        Task<Profile?> GetByIdAsync(Guid profileId);
        Task<Profile?> GetByAccountIdAsync(Guid accountId);
        Task<List<Profile>> GetByIdsAsync(IEnumerable<Guid> profileIds);
        /// <summary>
        /// For forgot-password OTP: returns profile <see cref="Profile.FullName"/> when there is an email credential
        /// for an active, non-deleted account with a profile; otherwise <c>null</c> (normalized email).
        /// </summary>
        Task<string?> GetFullNameForEligibleForgotPasswordEmailAsync(string normalizedEmail, CancellationToken ct = default);
    }
}
