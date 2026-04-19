using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories;

public interface IStockMovementRepository
{
    /// <summary>
    /// Get stock movements for a business location with Product navigation loaded.
    /// Used by FormulaEngine (aggregate) and BookRenderingService (data rows).
    /// </summary>
    Task<List<StockMovement>> GetByLocationAsync(int businessLocationId);

    /// <summary>
    /// Get stock movements for a location filtered by date range and optionally by product.
    /// Used by FormulaEngine aggregate to avoid loading the entire table into memory.
    /// </summary>
    Task<List<StockMovement>> GetByLocationAndPeriodAsync(
        int businessLocationId,
        DateOnly? from,
        DateOnly? to,
        long? productId = null);
}
