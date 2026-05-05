using BizFlow.Application.DTOs.Debtor;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Specifications.Debtors;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using BizFlow.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class DebtorRepository : IDebtorRepository
    {
        private readonly BizFlowDbContext _db;

        public DebtorRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Debtor> Items, int TotalCount)> SearchAsync(
            DebtorQueryParams query,
            IEnumerable<int>? allowedLocationIds = null
        )
        {
            // 1. Get Total Count
            var countSpec = new DebtorSearchSpec(query, allowedLocationIds, isCount: true);
            var countQuery = SpecificationEvaluator<Debtor>.GetQuery(
                _db.Debtors.AsQueryable(),
                countSpec
            );
            var totalCount = await countQuery.CountAsync();

            if (totalCount == 0)
                return (Array.Empty<Debtor>(), 0);

            // 2. Deferred Join Strategy (Get IDs first)
            var filterSpec = new DebtorSearchSpec(
                query,
                allowedLocationIds,
                isCount: false,
                filterOnly: true
            );
            var filterQuery = SpecificationEvaluator<Debtor>.GetQuery(
                _db.Debtors.AsQueryable(),
                filterSpec
            );

            var pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
            var pageSize = query.PageSize > 0 ? query.PageSize : 20;

            var ids = await filterQuery
                .Select(d => d.DebtorId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!ids.Any())
                return (Array.Empty<Debtor>(), totalCount);

            // 3. Fetch full entities
            var items = await _db
                .Debtors.Where(d => ids.Contains(d.DebtorId))
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task<Debtor?> GetByIdAsync(long debtorId) =>
            _db.Debtors.FirstOrDefaultAsync(d => d.DebtorId == debtorId);

        public Task<Debtor?> GetByIdWithTransactionsAsync(long debtorId) =>
            _db
                .Debtors.Include(d => d.DebtorPaymentTransactions)
                .FirstOrDefaultAsync(d => d.DebtorId == debtorId);

        public Task<bool> PhoneExistsInLocationAsync(
            int locationId,
            string phone,
            long? excludeDebtorId = null
        ) =>
            _db.Debtors.AnyAsync(d =>
                d.BusinessLocationId == locationId
                && d.Phone == phone
                && (excludeDebtorId == null || d.DebtorId != excludeDebtorId)
            );

        public async Task<IEnumerable<Debtor>> GetActiveByLocationAsync(int locationId) =>
            await _db
                .Debtors.Where(d => d.BusinessLocationId == locationId && d.IsActive == true)
                .OrderBy(d => d.Name)
                .ToListAsync();

        public async Task<Debtor> AddAsync(Debtor debtor)
        {
            _db.Debtors.Add(debtor);
            return debtor;
        }

        public void Update(Debtor debtor) => _db.Debtors.Update(debtor);

        public void Remove(Debtor debtor) => _db.Debtors.Remove(debtor);

        public async Task<bool> HasAnyActivityAsync(long debtorId) =>
            await _db.Debtors.AnyAsync(d =>
                d.DebtorId == debtorId && (d.Orders.Any() || d.DebtorPaymentTransactions.Any())
            );

        public async Task<DebtorPaymentTransaction> AddPaymentAsync(
            DebtorPaymentTransaction transaction
        )
        {
            _db.DebtorPaymentTransactions.Add(transaction);
            return transaction;
        }

        public async Task<IEnumerable<DebtorPaymentTransaction>> GetPaymentsAsync(long debtorId) =>
            await _db
                .DebtorPaymentTransactions.Where(t => t.DebtorId == debtorId)
                .OrderByDescending(t => t.PaidAt)
                .ToListAsync();

        public async Task<IEnumerable<DebtorPaymentTransaction>> GetPaymentsByIdsAsync(
            IReadOnlyCollection<long> paymentIds
        )
        {
            if (paymentIds == null || paymentIds.Count == 0)
                return [];

            return await _db
                .DebtorPaymentTransactions.Where(t =>
                    paymentIds.Contains(t.DebtorPaymentTransactionId)
                )
                .ToListAsync();
        }

        public async Task<(int MatchedCount, int UpdatedCount)> SyncCurrentBalancesAsync(
            long? debtorId = null,
            CancellationToken cancellationToken = default
        )
        {
            var debtorQuery = _db.Debtors.AsQueryable();
            if (debtorId.HasValue)
                debtorQuery = debtorQuery.Where(d => d.DebtorId == debtorId.Value);

            var debtors = await debtorQuery.ToListAsync(cancellationToken);
            if (debtors.Count == 0)
                return (0, 0);

            var debtorIds = debtors.Select(d => d.DebtorId).ToList();

            var deltaByDebtorId = await _db
                .DebtorPaymentTransactions.Where(t => debtorIds.Contains(t.DebtorId))
                .GroupBy(t => t.DebtorId)
                .Select(g => new
                {
                    DebtorId = g.Key,
                    Delta = g.Sum(x => x.BalanceAfter - x.BalanceBefore),
                })
                .ToDictionaryAsync(x => x.DebtorId, x => x.Delta, cancellationToken);

            var now = DateTime.UtcNow;
            var updatedCount = 0;
            foreach (var debtor in debtors)
            {
                deltaByDebtorId.TryGetValue(debtor.DebtorId, out var expectedBalance);

                if (debtor.CurrentBalance == expectedBalance)
                    continue;

                debtor.CurrentBalance = expectedBalance;
                debtor.UpdatedAt = now;
                updatedCount++;
            }

            if (updatedCount > 0)
                await _db.SaveChangesAsync(cancellationToken);

            return (debtors.Count, updatedCount);
        }

        public async Task<decimal> SumDebtBalanceChangeByLocationsAndPaidAtUtcRangeAsync(
            IReadOnlyCollection<int> businessLocationIds,
            DateTime fromUtcInclusive,
            DateTime toUtcInclusive,
            CancellationToken cancellationToken = default
        )
        {
            if (businessLocationIds == null || businessLocationIds.Count == 0)
                return 0m;

            return await (
                from t in _db.DebtorPaymentTransactions
                join d in _db.Debtors on t.DebtorId equals d.DebtorId
                where businessLocationIds.Contains(d.BusinessLocationId)
                    && d.IsActive == true
                    && t.PaidAt >= fromUtcInclusive
                    && t.PaidAt <= toUtcInclusive
                select t.BalanceAfter - t.BalanceBefore
            ).SumAsync(cancellationToken);
        }
    }
}
