using BizFlow.Application.DTOs.Revenue;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IRevenueRepository
    {
        Task<(IEnumerable<Revenue> Items, int TotalCount)> SearchAsync(RevenueQueryParams query);
        Task<Revenue?> GetByIdAsync(long revenueId);
        Task<List<Revenue>> GetSaleByOrderIdAsync(int businessLocationId, long orderId);
        Task<Revenue> AddAsync(Revenue revenue);
        void Update(Revenue revenue);
    }
}
