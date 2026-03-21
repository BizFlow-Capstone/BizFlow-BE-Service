using BizFlow.Application.DTOs.Order;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        Task<(IEnumerable<Order> Items, int TotalCount)> SearchAsync(OrderQueryParams query);
        Task<Order?> GetByIdAsync(long orderId);
        Task<Order?> GetByIdWithDetailsAsync(long orderId);
        Task<Order?> GetLatestReplacementByRefOrderIdAsync(long refOrderId);
        Task<Order?> GetLatestReplacementByRefOrderIdAsync(long refOrderId, string idempotencyMarker);
        Task<Order?> GetByCodeAsync(string orderCode);
        Task<Order> AddAsync(Order order);
        void Update(Order order);
        void Remove(Order order);
    }
}
