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
            return await _db.AiProductInsights
                .Where(i => i.LocationId == locationId)
                .OrderBy(i => i.InsightType)
                .ThenBy(i => i.Rank)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
