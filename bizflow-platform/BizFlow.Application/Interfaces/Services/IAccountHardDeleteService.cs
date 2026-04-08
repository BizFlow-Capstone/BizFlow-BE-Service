namespace BizFlow.Application.Interfaces.Services;

/// <summary>
/// Physically removes a soft-deleted account after retention: anonymizes audit FKs to the profile,
/// deletes owned business locations, billing rows, then the account row.
/// </summary>
public interface IAccountHardDeleteService
{
    /// <summary>Processes up to <c>BatchSize</c> candidates; returns how many were physically purged (committed).</summary>
    Task<int> ProcessPendingHardDeletesAsync(CancellationToken cancellationToken = default);
}
