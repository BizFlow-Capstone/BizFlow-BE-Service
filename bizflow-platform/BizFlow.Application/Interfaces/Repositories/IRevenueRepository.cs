using BizFlow.Application.DTOs.Revenue;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IRevenueRepository
    {
        Task<(IEnumerable<Revenue> Items, int TotalCount)> SearchAsync(RevenueQueryParams query);
        Task<Revenue?> GetByIdAsync(long revenueId);
        Task<List<Revenue>> GetSaleByOrderIdAsync(int businessLocationId, long orderId);
        Task<IEnumerable<Revenue>> GetByIdsAsync(IEnumerable<long> revenueIds);
        Task<Revenue> AddAsync(Revenue revenue);
        void Update(Revenue revenue);

        Task<decimal> SumAmountByLocationsAndDateRangeAsync(
            IReadOnlyCollection<int> businessLocationIds,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default);

        // ── Formula Engine aggregation (avoids PageSize = int.MaxValue) ──
        Task<decimal> AggregateByLocationAndPeriodAsync(
            int locationId, DateOnly from, DateOnly to,
            string aggType, string[] revenueTypes);

        Task<Dictionary<string, decimal>> SumGroupedByBusinessTypeAsync(
            int locationId, DateOnly from, DateOnly to);
    }
}
