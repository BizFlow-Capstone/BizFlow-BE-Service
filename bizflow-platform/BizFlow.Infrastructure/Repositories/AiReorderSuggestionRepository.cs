using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class AiReorderSuggestionRepository : IAiReorderSuggestionRepository
    {
        private readonly BizFlowDbContext _db;

        public AiReorderSuggestionRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<List<AiReorderSuggestion>> GetByLocationAsync(string locationId)
        {
            // Return only the latest computed batch (max generated_at day).
            var maxGeneratedAt = await _db.AiReorderSuggestions
                .Where(r => r.LocationId == locationId)
                .MaxAsync(r => (DateTime?)r.GeneratedAt);

            if (maxGeneratedAt == null)
                return new List<AiReorderSuggestion>();

            var batchStart = maxGeneratedAt.Value.Date;

            return await _db.AiReorderSuggestions
                .Where(r => r.LocationId == locationId && r.GeneratedAt >= batchStart)
                .OrderBy(r => r.DaysUntilStockout)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<AiReorderSuggestion>> GetByLocationAndUrgencyAsync(string locationId, string urgency)
        {
            return await _db.AiReorderSuggestions
                .Where(r => r.LocationId == locationId && r.Urgency == urgency)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
