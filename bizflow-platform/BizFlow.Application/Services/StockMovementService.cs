using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services
{
    public class StockMovementService : IStockMovementService
    {
        public StockMovement CreateStockMovement(
            Product product,
            StockMovementType movementType,
            int quantity,
            StockMovementReferenceType? referenceType,
            long? referenceId)
        {
            return new StockMovement
            {
                ProductId = product.ProductId,
                MovementType = ToMovementTypeValue(movementType),
                Quantity = quantity,
                ReferenceType = referenceType.HasValue ? ToReferenceTypeValue(referenceType.Value) : null,
                ReferenceId = referenceId,
                BalanceAfter = product.Stock,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static string ToMovementTypeValue(StockMovementType movementType)
        {
            return movementType switch
            {
                StockMovementType.In => "IN",
                StockMovementType.Out => "OUT",
                StockMovementType.Adjustment => "ADJUSTMENT",
                _ => throw new ArgumentOutOfRangeException(nameof(movementType), movementType, null)
            };
        }

        private static string ToReferenceTypeValue(StockMovementReferenceType referenceType)
        {
            return referenceType switch
            {
                StockMovementReferenceType.Import => "IMPORT",
                StockMovementReferenceType.Adjustment => "ADJUSTMENT",
                _ => throw new ArgumentOutOfRangeException(nameof(referenceType), referenceType, null)
            };
        }
    }
}
