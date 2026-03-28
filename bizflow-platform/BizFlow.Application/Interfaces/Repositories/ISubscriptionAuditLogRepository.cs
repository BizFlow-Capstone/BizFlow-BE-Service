using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface ISubscriptionAuditLogRepository
    {
        Task AddAsync(SubscriptionAuditLog log);
    }
}
