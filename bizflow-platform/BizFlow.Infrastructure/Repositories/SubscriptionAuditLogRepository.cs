using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;

namespace BizFlow.Infrastructure.Repositories
{
    public class SubscriptionAuditLogRepository : ISubscriptionAuditLogRepository
    {
        private readonly BizFlowDbContext _context;

        public SubscriptionAuditLogRepository(BizFlowDbContext context)
        {
            _context = context;
        }

        public Task AddAsync(SubscriptionAuditLog log)
        {
            return _context.SubscriptionAuditLogs.AddAsync(log).AsTask();
        }
    }
}
