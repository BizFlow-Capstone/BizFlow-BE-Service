using BizFlow.Application.DTOs.Cost;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ICostRepository
    {
        Task<(IEnumerable<Cost> Items, int TotalCount)> SearchAsync(CostQueryParams query);
        Task<Cost?> GetByIdAsync(long costId);
        Task<Cost?> GetByImportIdAsync(long importId);
        Task<IEnumerable<Cost>> GetByIdsAsync(IEnumerable<long> costIds);
        Task<Cost> AddAsync(Cost cost);
        void Update(Cost cost);

        Task<decimal> SumAmountByLocationsAndDateRangeAsync(
            IReadOnlyCollection<int> businessLocationIds,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default);
    }
}
