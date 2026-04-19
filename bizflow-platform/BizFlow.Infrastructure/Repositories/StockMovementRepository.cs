using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly BizFlowDbContext _db;

    public StockMovementRepository(BizFlowDbContext db)
    {
        _db = db;
    }

    public async Task<List<StockMovement>> GetByLocationAsync(int businessLocationId)
    {
        return await _db.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => sm.Product.BusinessLocationId == businessLocationId
                         && sm.Product.DeletedAt == null)
            .OrderBy(sm => sm.CreatedAt)
            .ThenBy(sm => sm.StockMovementId)
            .ToListAsync();
    }

    public async Task<List<StockMovement>> GetByLocationAndPeriodAsync(
        int businessLocationId,
        DateOnly? from,
        DateOnly? to,
        long? productId = null)
    {
        var query = _db.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => sm.Product.BusinessLocationId == businessLocationId
                         && sm.Product.DeletedAt == null);

        if (from.HasValue)
        {
            var fromDt = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(sm => sm.CreatedAt >= fromDt);
        }

        if (to.HasValue)
        {
            var toDt = to.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(sm => sm.CreatedAt <= toDt);
        }

        if (productId.HasValue)
            query = query.Where(sm => sm.ProductId == productId.Value);

        return await query
            .OrderBy(sm => sm.CreatedAt)
            .ThenBy(sm => sm.StockMovementId)
            .ToListAsync();
    }
}
