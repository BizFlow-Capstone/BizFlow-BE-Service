using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class AiAnomalyAlertRepository : IAiAnomalyAlertRepository
    {
        private readonly BizFlowDbContext _db;

        public AiAnomalyAlertRepository(BizFlowDbContext db)
        {
            _db = db;
        }

        public async Task<List<AiAnomalyAlert>> GetByLocationAsync(string locationId, bool? acknowledged = null)
        {
            var query = _db.AiAnomalyAlerts
                .Where(a => a.LocationId == locationId);

            if (acknowledged.HasValue)
                query = query.Where(a => a.IsAcknowledged == acknowledged.Value);

            return await query
                .OrderByDescending(a => a.GeneratedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<AiAnomalyAlert?> GetByIdAsync(string id)
        {
            return await _db.AiAnomalyAlerts.FindAsync(id);
        }

        public void Update(AiAnomalyAlert alert)
        {
            _db.AiAnomalyAlerts.Update(alert);
        }
    }
}
