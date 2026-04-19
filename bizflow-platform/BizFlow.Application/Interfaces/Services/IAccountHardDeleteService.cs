namespace BizFlow.Application.Interfaces.Services;

/// <summary>
/// Physically removes a soft-deleted account after retention: anonymizes audit FKs to the profile,
/// deletes owned business locations, billing rows, then the account row.
/// </summary>
public interface IAccountHardDeleteService
{
    /// <summary>
    /// Immediately hard-deletes one account in a resilient transaction.
    /// Returns <c>true</c> when the account was physically removed.
    /// </summary>
    Task<bool> HardDeleteAccountNowAsync(Guid accountId, CancellationToken cancellationToken = default);
}
