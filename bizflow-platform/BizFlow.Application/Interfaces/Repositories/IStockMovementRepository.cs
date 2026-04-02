using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IStockMovementRepository
{
    /// <summary>
    /// Get stock movements for a business location with Product navigation loaded.
    /// Used by FormulaEngine (aggregate) and BookRenderingService (data rows).
    /// </summary>
    Task<List<StockMovement>> GetByLocationAsync(int businessLocationId);
}
