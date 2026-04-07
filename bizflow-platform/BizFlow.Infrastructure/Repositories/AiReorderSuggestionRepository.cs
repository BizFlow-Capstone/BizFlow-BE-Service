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
            return await _db.AiReorderSuggestions
                .Where(r => r.LocationId == locationId)
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
