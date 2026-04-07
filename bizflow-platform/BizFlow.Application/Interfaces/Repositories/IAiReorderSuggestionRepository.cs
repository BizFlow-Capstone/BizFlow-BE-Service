using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IAiReorderSuggestionRepository
    {
        Task<List<AiReorderSuggestion>> GetByLocationAsync(string locationId);
        Task<List<AiReorderSuggestion>> GetByLocationAndUrgencyAsync(string locationId, string urgency);
    }
}
