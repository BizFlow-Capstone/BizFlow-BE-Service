using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IStockMovementService
    {
        StockMovement CreateStockMovement(
            Product product,
            decimal quantityDelta,
            string? referenceType,
            long? referenceId,
            string? memo = null);
    }
}
