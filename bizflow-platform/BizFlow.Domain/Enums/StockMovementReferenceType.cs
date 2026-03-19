namespace BizFlow.Domain.Enums
{
    public static class StockMovementReferenceType
    {
        public const string Import = "IMPORT";
        public const string Order = "ORDER";
        public const string Adjustment = "ADJUSTMENT";

        public static readonly IReadOnlyList<string> All = [Import, Order, Adjustment];

        public static bool IsValid(string type) =>
            All.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}
