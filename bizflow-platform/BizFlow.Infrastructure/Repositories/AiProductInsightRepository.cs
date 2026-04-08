using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class AiProductInsightRepository : IAiProductInsightRepository
    {
        private readonly BizFlowDbContext _db;

        public AiProductInsightRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<List<AiProductInsight>> GetByLocationAsync(string locationId)
        {
            // Return only the latest computed batch (max generated_at day).
            var maxGeneratedAt = await _db.AiProductInsights
                .Where(i => i.LocationId == locationId)
                .MaxAsync(i => (DateTime?)i.GeneratedAt);

            if (maxGeneratedAt == null)
                return new List<AiProductInsight>();

            var batchStart = maxGeneratedAt.Value.Date;

            return await _db.AiProductInsights
                .Where(i => i.LocationId == locationId && i.GeneratedAt >= batchStart)
                .OrderBy(i => i.InsightType)
                .ThenBy(i => i.Rank)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
