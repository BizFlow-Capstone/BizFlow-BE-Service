using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    /// <summary>
    /// Repository for <see cref="AccountingDocumentLock"/> — the synthetic row-lock
    /// table used to serialize DocumentNumber uniqueness checks per Owner.
    /// </summary>
    public interface IAccountingDocumentLockRepository
    {
        /// <summary>
        /// Get the lock row for a given (Owner, DocumentNumberNormalized) pair, if any.
        /// </summary>
        Task<AccountingDocumentLock?> GetAsync(Guid ownerId, string documentNumberNormalized, CancellationToken cancellationToken = default);

        /// <summary>
        /// Insert a new lock row. Caller is responsible for catching duplicate-key
        /// violations (which can legitimately happen under concurrency) and falling
        /// back to a lookup.
        /// </summary>
        Task<AccountingDocumentLock> AddAsync(AccountingDocumentLock entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomic "get-or-create" for the lock row. Uses <c>INSERT ... ON DUPLICATE KEY UPDATE</c>
        /// semantics via raw SQL so callers always have a lockable row.
        /// The returned LockId is the one currently stored in DB (newly inserted or pre-existing).
        /// </summary>
        Task<Guid> EnsureExistsAsync(Guid ownerId, string documentNumberNormalized, CancellationToken cancellationToken = default);

        /// <summary>
        /// Must run inside an open DB transaction. Acquires an InnoDB row-level
        /// write lock on the (Owner, DocumentNumberNormalized) pair via
        /// <c>SELECT ... FOR UPDATE</c>. Call <see cref="EnsureExistsAsync"/> first.
        /// </summary>
        Task LockForUpdateAsync(Guid ownerId, string documentNumberNormalized, CancellationToken cancellationToken = default);
    }
}
