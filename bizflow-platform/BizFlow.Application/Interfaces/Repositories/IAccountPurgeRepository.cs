namespace BizFlow.Application.Interfaces.Repositories;

/// <summary>
/// Data access for background account hard-delete: anonymize audit FKs, purge owned locations, remove account row.
/// Must run inside an active EF transaction (see <see cref="IUnitOfWork.ExecuteResilientPurgeAsync"/>).
/// </summary>
public interface IAccountPurgeRepository
{
    /// <summary>Soft-deleted accounts ready for physical purge (by <c>DeletedAt</c> cutoff).</summary>
    Task<List<Guid>> GetPendingPurgeAccountIdsAsync(DateTime eligibilityCutoffUtc, int take, CancellationToken ct = default);

    /// <summary>
    /// <c>Committed</c>: purge ran and transaction should commit.
    /// <c>PurgedProfileId</c>: profile id when a profile existed; <c>null</c> when orphan account only.
    /// </summary>
    Task<(bool Committed, Guid? PurgedProfileId)> TryPurgeOneAccountWithinTransactionAsync(
        Guid accountId,
        DateTime eligibilityCutoffUtc,
        CancellationToken ct = default);
}
