using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IStockMovementService
    {
        StockMovement CreateStockMovement(
            Product product,
            StockMovementType movementType,
            int quantity,
            StockMovementReferenceType? referenceType,
            long? referenceId);
    }
}
