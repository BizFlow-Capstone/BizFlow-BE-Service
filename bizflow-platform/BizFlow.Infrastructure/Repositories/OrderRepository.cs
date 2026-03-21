using BizFlow.Application.DTOs.Order;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using BizFlow.Application.Specifications.Orders;
using BizFlow.Infrastructure.Specifications;
using System.Text.Json;
using System.Text.Json.Nodes;

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
            // 1. Get total count
            var countSpec = new OrderSearchSpec(query, isCount: true);
            var countQuery = SpecificationEvaluator<Order>.GetQuery(_db.Orders.AsQueryable(), countSpec);
            var totalCount = await countQuery.CountAsync();

            if (totalCount == 0)
                return (Array.Empty<Order>(), 0);

            // 2. Deferred Join Strategy (Get IDs first)
            var filterSpec = new OrderSearchSpec(query, isCount: false, filterOnly: true);
            var filterQuery = SpecificationEvaluator<Order>.GetQuery(_db.Orders.AsQueryable(), filterSpec);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;

            var ids = await filterQuery
                .Select(o => o.OrderId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!ids.Any())
                return (Array.Empty<Order>(), totalCount);

            // 3. Fetch full entities
            var items = await _db.Orders
                .Where(o => ids.Contains(o.OrderId))
                .OrderByDescending(o => o.CreatedAt)
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

        public async Task<Order?> GetLatestReplacementByRefOrderIdAsync(long refOrderId, string idempotencyMarker)
        {
            var candidates = await _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.SaleItem)
                        .ThenInclude(si => si.Product)
                .Where(o => o.RefOrderId == refOrderId)
                .OrderByDescending(o => o.OrderId)
                .Take(50)
                .ToListAsync();

            return candidates.FirstOrDefault(o => HasIdempotencyMarker(o.BillMetadata, idempotencyMarker));
        }

        private static bool HasIdempotencyMarker(string? billMetadata, string marker)
        {
            if (string.IsNullOrWhiteSpace(billMetadata))
                return false;

            try
            {
                var node = JsonNode.Parse(billMetadata);
                if (node is JsonObject obj)
                {
                    var markerValue = obj["idempotencyMarker"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(markerValue)
                        && string.Equals(markerValue, marker, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return billMetadata.Contains(marker, StringComparison.Ordinal);
            }
            catch (JsonException)
            {
                // Legacy bad data: fallback to raw string match.
                return billMetadata.Contains(marker, StringComparison.Ordinal);
            }
        }

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
