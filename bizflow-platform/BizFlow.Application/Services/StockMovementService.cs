using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services
{
    public class StockMovementService : IStockMovementService
    {
        #region Command Methods

        public StockMovement CreateStockMovement(
            Product product,
            int quantityDelta,
            StockMovementReferenceType? referenceType,
            long? referenceId,
            string? memo = null)
        {
            if (quantityDelta == 0)
                throw new BadRequestException(MessageKeys.BadRequest);

            var movementType = quantityDelta > 0
                ? StockMovementType.In
                : StockMovementType.Out;

            return new StockMovement
            {
                ProductId = product.ProductId,
                MovementType = movementType.ToDbValue(),
                Quantity = quantityDelta,
                ReferenceType = referenceType.HasValue ? referenceType.Value.ToDbValue() : null,
                ReferenceId = referenceId,
                Memo = memo,
                BalanceAfter = product.Stock,
                CreatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
}
