using BizFlow.Application.DTOs.Debtor;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IDebtorRepository
    {
        Task<(IEnumerable<Debtor> Items, int TotalCount)> SearchAsync(DebtorQueryParams query, IEnumerable<int>? allowedLocationIds = null);
        Task<Debtor?> GetByIdAsync(long debtorId);
        Task<Debtor?> GetByIdWithTransactionsAsync(long debtorId);
        Task<bool> PhoneExistsInLocationAsync(int locationId, string phone, long? excludeDebtorId = null);
        Task<IEnumerable<Debtor>> GetActiveByLocationAsync(int locationId);
        Task<Debtor> AddAsync(Debtor debtor);
        void Update(Debtor debtor);
        void Remove(Debtor debtor);
        Task<bool> HasAnyActivityAsync(long debtorId);

        Task<DebtorPaymentTransaction> AddPaymentAsync(DebtorPaymentTransaction transaction);
        Task<IEnumerable<DebtorPaymentTransaction>> GetPaymentsAsync(long debtorId);
        Task<IEnumerable<DebtorPaymentTransaction>> GetPaymentsByIdsAsync(IReadOnlyCollection<long> paymentIds);
        Task<(int MatchedCount, int UpdatedCount)> SyncCurrentBalancesAsync(
            long? debtorId = null,
            CancellationToken cancellationToken = default);

        Task<decimal> SumDebtBalanceChangeByLocationsAndPaidAtUtcRangeAsync(
            IReadOnlyCollection<int> businessLocationIds,
            DateTime fromUtcInclusive,
            DateTime toUtcInclusive,
            CancellationToken cancellationToken = default);
    }
}
