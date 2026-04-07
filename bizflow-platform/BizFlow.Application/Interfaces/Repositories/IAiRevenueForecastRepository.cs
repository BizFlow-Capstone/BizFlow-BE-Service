using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IAiRevenueForecastRepository
    {
        Task<List<AiRevenueForecast>> GetByLocationAsync(string locationId);
    }
}
