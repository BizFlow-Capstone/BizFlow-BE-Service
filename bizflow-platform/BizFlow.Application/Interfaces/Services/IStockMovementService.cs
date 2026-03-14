using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IStockMovementService
    {
        StockMovement CreateStockMovement(
            Product product,
            int quantityDelta,
            StockMovementReferenceType? referenceType,
            long? referenceId,
            string? memo = null);
    }
}
