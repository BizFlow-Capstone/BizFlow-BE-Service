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
}
