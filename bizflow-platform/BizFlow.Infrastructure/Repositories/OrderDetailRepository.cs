using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;

namespace BizFlow.Infrastructure.Repositories
{
    public class OrderDetailRepository : IOrderDetailRepository
    {
        private readonly BizFlowDbContext _db;

        public OrderDetailRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<OrderDetail> AddAsync(OrderDetail orderDetail)
        {
            _db.OrderDetails.Add(orderDetail);
            return orderDetail;
        }

        public async Task AddRangeAsync(IEnumerable<OrderDetail> orderDetails)
        {
            _db.OrderDetails.AddRange(orderDetails);
        }

        public void RemoveRange(IEnumerable<OrderDetail> orderDetails)
            => _db.OrderDetails.RemoveRange(orderDetails);
    }
}
