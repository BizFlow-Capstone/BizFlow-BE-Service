using BizFlow.Application.DTOs.Admin;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

/// <summary>
/// Queries <see cref="Account"/> (credentials, profile, role) and related refresh tokens.
/// </summary>
public interface IAccountRepository
{
    Task<(IEnumerable<Account> Items, int TotalCount)> SearchAdminManagedUsersAsync(AdminUserQueryParams query);

    Task<Account?> GetNonAdminAccountByIdAsync(Guid accountId);

    Task<List<RefreshToken>> GetUnexpiredRefreshTokensAsync(Guid accountId, DateTime nowUtc);

    /// <summary>
    /// Account with Role and Profile via email credential (<paramref name="normalizedEmail"/> already lowercased). Tracked for updates.
    /// </summary>
    Task<Account?> GetWithProfileAndRoleByNormalizedEmailCredentialAsync(string normalizedEmail, CancellationToken ct = default);

    /// <summary>Tracked account by primary key (for password / token field updates).</summary>
    Task<Account?> GetTrackedByIdAsync(Guid accountId, CancellationToken ct = default);
}
