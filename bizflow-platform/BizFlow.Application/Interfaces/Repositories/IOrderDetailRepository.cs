using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IOrderDetailRepository
    {
        Task<OrderDetail> AddAsync(OrderDetail orderDetail);
        Task AddRangeAsync(IEnumerable<OrderDetail> orderDetails);
        void RemoveRange(IEnumerable<OrderDetail> orderDetails);
    }
}
