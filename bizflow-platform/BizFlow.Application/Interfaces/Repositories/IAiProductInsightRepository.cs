using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IAiProductInsightRepository
    {
        Task<List<AiProductInsight>> GetByLocationAsync(string locationId);
    }
}
