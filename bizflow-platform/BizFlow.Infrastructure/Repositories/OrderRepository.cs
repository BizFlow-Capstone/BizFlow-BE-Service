using BizFlow.Application.DTOs.Order;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly BizFlowDbContext _db;

        public OrderRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Order> Items, int TotalCount)> SearchAsync(OrderQueryParams query)
        {
            var q = _db.Orders
                .Where(o => o.OrderDetails.Any(od => od.SaleItem.Product.BusinessLocationId == query.BusinessLocationId))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLower();
                q = q.Where(o => o.Status.ToLower() == status);
            }

            if (query.FromDate.HasValue)
                q = q.Where(o => o.CreatedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                q = q.Where(o => o.CreatedAt <= query.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(o => o.OrderCode.Contains(s)
                    || (o.CustomerName != null && o.CustomerName.Contains(s))
                    || (o.CustomerPhone != null && o.CustomerPhone.Contains(s)));
            }

            var totalCount = await q.CountAsync();
            if (totalCount == 0)
                return (Array.Empty<Order>(), 0);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;

            var items = await q
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.SaleItem)
                        .ThenInclude(si => si.Product)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task<Order?> GetByIdAsync(long orderId)
            => _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

        public Task<Order?> GetByIdWithDetailsAsync(long orderId)
            => _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.SaleItem)
                        .ThenInclude(si => si.Product)
                .Include(o => o.Debtor)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

        public Task<Order?> GetLatestReplacementByRefOrderIdAsync(long refOrderId)
            => _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.SaleItem)
                        .ThenInclude(si => si.Product)
                .Where(o => o.RefOrderId == refOrderId)
                .OrderByDescending(o => o.OrderId)
                .FirstOrDefaultAsync();

        public Task<Order?> GetLatestReplacementByRefOrderIdAsync(long refOrderId, string idempotencyMarker)
            => _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.SaleItem)
                        .ThenInclude(si => si.Product)
                .Where(o => o.RefOrderId == refOrderId
                    && o.BillMetadata != null
                    && o.BillMetadata.Contains(idempotencyMarker))
                .OrderByDescending(o => o.OrderId)
                .FirstOrDefaultAsync();

        public Task<Order?> GetByCodeAsync(string orderCode)
            => _db.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        public async Task<Order> AddAsync(Order order)
        {
            _db.Orders.Add(order);
            return order;
        }

        public void Update(Order order)
            => _db.Orders.Update(order);

        public void Remove(Order order)
            => _db.Orders.Remove(order);
    }
}
