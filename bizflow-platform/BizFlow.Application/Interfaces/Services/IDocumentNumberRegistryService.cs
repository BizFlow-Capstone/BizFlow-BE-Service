namespace BizFlow.Application.Interfaces.Services
{
    /// <summary>
    /// Enforces the "DocumentNumber is unique per Owner across Costs + Revenues
    /// (including cancelled / replaced rows)" invariant.
    ///
    /// <para>
    /// Usage contract:
    /// <list type="number">
    ///   <item>Call <see cref="NormalizeOrNull"/> to canonicalize user input.</item>
    ///   <item>Inside <c>IUnitOfWork.ExecuteResilientAsync</c>, call
    ///     <see cref="EnsureLockAndAssertUniqueAsync"/> BEFORE writing the Cost/Revenue row.</item>
    /// </list>
    /// </para>
    /// </summary>
    public interface IDocumentNumberRegistryService
    {
        /// <summary>
        /// Returns the normalized form of <paramref name="documentNumber"/>
        /// (upper-cased, trimmed, collapsed inner whitespace).
        /// Returns <c>null</c> if the input is null/blank (document numbers are
        /// optional and null values bypass the uniqueness check).
        /// </summary>
        /// <exception cref="BizFlow.Application.Common.Exceptions.BadRequestException">
        /// Thrown when the normalized value exceeds 100 characters.
        /// </exception>
        string? NormalizeOrNull(string? documentNumber);

        /// <summary>
        /// Must be called inside an OPEN DB transaction (e.g. inside
        /// <c>_uow.ExecuteResilientAsync</c>).
        /// <para>
        /// 1. Ensures an <see cref="BizFlow.Domain.Entities.AccountingDocumentLock"/>
        /// row exists for (<paramref name="ownerId"/>, normalized doc number).
        /// </para>
        /// <para>
        /// 2. Acquires a <c>SELECT ... FOR UPDATE</c> row lock on that row to
        /// serialize concurrent peers.
        /// </para>
        /// <para>
        /// 3. Queries Costs + Revenues (ignoring soft-delete/cancelled filters)
        /// for any existing row of this owner that uses the same normalized
        /// DocumentNumber. Throws <see cref="BizFlow.Application.Common.Exceptions.ConflictException"/>
        /// with <see cref="BizFlow.Application.Common.Constants.MessageKeys.DocumentNumberDuplicated"/>
        /// if a conflict is detected.
        /// </para>
        /// </summary>
        /// <param name="ownerId">Profile/Account id that owns the DocumentNumber namespace.</param>
        /// <param name="documentNumberNormalized">Normalized DocumentNumber. Call <see cref="NormalizeOrNull"/> first; pass non-null only.</param>
        /// <param name="excludeCostId">Optional Cost id to exclude from the check (the record we're editing in-place).</param>
        /// <param name="excludeRevenueId">Optional Revenue id to exclude from the check.</param>
        Task EnsureLockAndAssertUniqueAsync(
            Guid ownerId,
            string documentNumberNormalized,
            long? excludeCostId = null,
            long? excludeRevenueId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a document number is already used by the owner across
        /// Costs + Revenues (including cancelled/replaced rows).
        /// </summary>
        /// <param name="ownerId">Profile/Account id that owns the DocumentNumber namespace.</param>
        /// <param name="documentNumber">Raw document number input from client.</param>
        /// <param name="excludeCostId">Optional Cost id to exclude from the check.</param>
        /// <param name="excludeRevenueId">Optional Revenue id to exclude from the check.</param>
        /// <returns><c>true</c> when the normalized document number already exists.</returns>
        Task<bool> ExistsAsync(
            Guid ownerId,
            string? documentNumber,
            long? excludeCostId = null,
            long? excludeRevenueId = null,
            CancellationToken cancellationToken = default);
    }
}
