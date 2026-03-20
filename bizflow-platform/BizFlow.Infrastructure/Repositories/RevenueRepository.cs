using BizFlow.Application.DTOs.Revenue;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class RevenueRepository : IRevenueRepository
    {
        private readonly BizFlowDbContext _db;

        public RevenueRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Revenue> Items, int TotalCount)> SearchAsync(RevenueQueryParams query)
        {
            var q = _db.Revenues
                .Where(r => r.BusinessLocationId == query.BusinessLocationId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.RevenueType))
            {
                var type = query.RevenueType.Trim().ToLower();
                q = q.Where(r => r.RevenueType.ToLower() == type);
            }

            if (!string.IsNullOrWhiteSpace(query.MoneyChannel))
            {
                var channel = query.MoneyChannel.Trim().ToLower();
                q = q.Where(r => r.MoneyChannel != null && r.MoneyChannel.ToLower() == channel);
            }

            if (query.FromDate.HasValue)
                q = q.Where(r => r.RevenueDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                q = q.Where(r => r.RevenueDate <= query.ToDate.Value);

            var total = await q.CountAsync();
            if (total == 0)
                return (Array.Empty<Revenue>(), 0);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 20;

            var items = await q
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public Task<Revenue?> GetByIdAsync(long revenueId)
            => _db.Revenues.FirstOrDefaultAsync(r => r.RevenueId == revenueId);

        public async Task<List<Revenue>> GetSaleByOrderIdAsync(int businessLocationId, long orderId)
        {
            var marker = $"ORDER#{orderId}";
            return await _db.Revenues
                .Where(r => r.BusinessLocationId == businessLocationId
                    && r.RevenueType == "sale"
                    && r.DeletedAt == null
                    && r.Description.StartsWith(marker))
                .OrderBy(r => r.RevenueId)
                .ToListAsync();
        }

        public async Task<Revenue> AddAsync(Revenue revenue)
        {
            _db.Revenues.Add(revenue);
            return revenue;
        }

        public void Update(Revenue revenue)
            => _db.Revenues.Update(revenue);
    }
}
