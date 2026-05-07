using BizFlow.Application.DTOs.Order;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        Task<(IEnumerable<Order> Items, int TotalCount)> SearchAsync(OrderQueryParams query);
        Task<Order?> GetByIdAsync(long orderId);
        Task<List<Order>> GetByIdsAsync(IEnumerable<long> orderIds);
        Task<Order?> GetByIdWithDetailsAsync(long orderId);

        /// <summary>Same as <see cref="GetByIdWithDetailsAsync"/> but no tracking — use before a transactional reload.</summary>
        Task<Order?> GetByIdWithDetailsAsNoTrackingAsync(long orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Must run inside an open DB transaction. MySQL InnoDB row lock (<c>SELECT ... FOR UPDATE</c>) so concurrent completes on the same order serialize.
        /// </summary>
        Task LockOrderRowForUpdateAsync(long orderId, CancellationToken cancellationToken = default);
        Task<Order?> GetLatestReplacementByRefOrderIdAsync(long refOrderId);
        Task<Order?> GetLatestReplacementByRefOrderIdAsync(long refOrderId, string idempotencyMarker);
        Task<Order?> GetByCodeAsync(string orderCode);
        Task<Order> AddAsync(Order order);
        void Update(Order order);
        void Remove(Order order);

        Task<int> CountCompletedByLocationsAndCompletedAtUtcAsync(
            IReadOnlyCollection<int> businessLocationIds,
            DateTime completedFromUtc,
            DateTime completedToUtc,
            CancellationToken cancellationToken = default);
    }
}
