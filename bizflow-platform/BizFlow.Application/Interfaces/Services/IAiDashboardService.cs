using BizFlow.Application.DTOs.Ai;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IAiDashboardService
    {
        Task<AiForecastReadDto> GetForecastAsync(int locationId);
        Task<List<AiReorderItemReadDto>> GetReorderSuggestionsAsync(int locationId);
        Task<List<AiProductInsightReadDto>> GetProductInsightsAsync(int locationId);
        Task<List<AiAnomalyAlertReadDto>> GetAnomaliesAsync(int locationId, bool? acknowledged);
        Task AcknowledgeAnomalyAsync(string id, int locationId);
    }
}
