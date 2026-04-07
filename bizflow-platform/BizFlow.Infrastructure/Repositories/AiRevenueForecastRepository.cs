using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class AiRevenueForecastRepository : IAiRevenueForecastRepository
    {
        private readonly BizFlowDbContext _db;

        public AiRevenueForecastRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<List<AiRevenueForecast>> GetByLocationAsync(string locationId)
        {
            return await _db.AiRevenueForecasts
                .Where(f => f.LocationId == locationId)
                .OrderBy(f => f.ForecastDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
