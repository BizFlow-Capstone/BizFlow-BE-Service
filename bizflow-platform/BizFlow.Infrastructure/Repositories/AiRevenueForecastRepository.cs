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
            // Return only the latest computed batch (max generated_at day).
            // Each job run inserts new UUID rows — this prevents stale batches from leaking in.
            var maxGeneratedAt = await _db.AiRevenueForecasts
                .Where(f => f.LocationId == locationId)
                .MaxAsync(f => (DateTime?)f.GeneratedAt);

            if (maxGeneratedAt == null)
                return new List<AiRevenueForecast>();

            var batchStart = maxGeneratedAt.Value.Date;

            return await _db.AiRevenueForecasts
                .Where(f => f.LocationId == locationId && f.GeneratedAt >= batchStart)
                .OrderBy(f => f.ForecastDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
