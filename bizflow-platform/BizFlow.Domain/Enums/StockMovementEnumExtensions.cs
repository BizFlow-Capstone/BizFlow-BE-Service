namespace BizFlow.Domain.Enums
{
    public static class StockMovementEnumExtensions
    {
        public static string ToDbValue(this StockMovementType movementType)
        {
            return movementType switch
            {
                StockMovementType.In => "IN",
                StockMovementType.Out => "OUT",
                StockMovementType.Adjustment => "ADJUSTMENT",
                _ => throw new ArgumentOutOfRangeException(nameof(movementType), movementType, null)
            };
        }

        public static string ToDbValue(this StockMovementReferenceType referenceType)
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
