using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IAiAnomalyAlertRepository
    {
        Task<List<AiAnomalyAlert>> GetByLocationAsync(string locationId, bool? acknowledged = null);
        Task<AiAnomalyAlert?> GetByIdAsync(string id);
        void Update(AiAnomalyAlert alert);
    }
}
