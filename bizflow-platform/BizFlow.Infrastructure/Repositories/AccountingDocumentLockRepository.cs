using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class AccountingDocumentLockRepository : IAccountingDocumentLockRepository
    {
        private readonly BizFlowDbContext _db;

        public AccountingDocumentLockRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public Task<AccountingDocumentLock?> GetAsync(Guid ownerId, string documentNumberNormalized, CancellationToken cancellationToken = default)
            => _db.AccountingDocumentLocks
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.OwnerId == ownerId && l.DocumentNumberNormalized == documentNumberNormalized,
                    cancellationToken);

        public Task<AccountingDocumentLock> AddAsync(AccountingDocumentLock entity, CancellationToken cancellationToken = default)
        {
            _db.AccountingDocumentLocks.Add(entity);
            return Task.FromResult(entity);
        }

        public async Task<Guid> EnsureExistsAsync(Guid ownerId, string documentNumberNormalized, CancellationToken cancellationToken = default)
        {
            // 1. Try fast path
            var existing = await _db.AccountingDocumentLocks
                .AsNoTracking()
                .Where(l => l.OwnerId == ownerId && l.DocumentNumberNormalized == documentNumberNormalized)
                .Select(l => (Guid?)l.LockId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing.HasValue)
                return existing.Value;

            // 2. Insert with INSERT IGNORE — safe under concurrency because of the unique index.
            //    Note: MySQL doesn't have a clean RETURNING, so we re-query after the insert.
            var newLockId = Guid.NewGuid();
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT IGNORE INTO AccountingDocumentLocks (LockId, OwnerId, DocumentNumberNormalized, CreatedAt) VALUES ({0}, {1}, {2}, NOW())",
                new object[] { newLockId.ToString(), ownerId.ToString(), documentNumberNormalized },
                cancellationToken);

            // 3. Re-read the authoritative LockId (may be the one we just inserted, or a racing peer's).
            var resolved = await _db.AccountingDocumentLocks
                .AsNoTracking()
                .Where(l => l.OwnerId == ownerId && l.DocumentNumberNormalized == documentNumberNormalized)
                .Select(l => l.LockId)
                .FirstAsync(cancellationToken);

            return resolved;
        }

        public Task LockForUpdateAsync(Guid ownerId, string documentNumberNormalized, CancellationToken cancellationToken = default)
            => _db.Database.ExecuteSqlRawAsync(
                "SELECT LockId FROM AccountingDocumentLocks WHERE OwnerId = {0} AND DocumentNumberNormalized = {1} LIMIT 1 FOR UPDATE",
                new object[] { ownerId.ToString(), documentNumberNormalized },
                cancellationToken);
    }
}
