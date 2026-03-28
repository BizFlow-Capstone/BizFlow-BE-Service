using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class SubscriptionPlanPriceRepository : ISubscriptionPlanPriceRepository
    {
        private readonly BizFlowDbContext _context;

        public SubscriptionPlanPriceRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public Task<SubscriptionPlanPrice?> GetActiveByPlanIdAsync(int subscriptionPlanId)
        {
            return _context.SubscriptionPlanPrices
                .FirstOrDefaultAsync(p => p.SubscriptionPlanId == subscriptionPlanId && p.IsActive);
        }

        public async Task AddAsync(SubscriptionPlanPrice price)
        {
            await _context.SubscriptionPlanPrices.AddAsync(price);
        }

        /// <summary>
        /// Bulk update DB; caller phải đồng bộ entity đang track (ExecuteUpdate không cập nhật tracker).
        /// </summary>
        public async Task DeactivateByPlanIdAsync(int subscriptionPlanId)
        {
            await _context.SubscriptionPlanPrices
                .Where(p => p.SubscriptionPlanId == subscriptionPlanId && p.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
        }
    }
}
