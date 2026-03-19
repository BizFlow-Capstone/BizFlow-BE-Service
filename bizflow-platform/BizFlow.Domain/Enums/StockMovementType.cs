namespace BizFlow.Domain.Enums
{
    public static class StockMovementType
    {
        public const string In = "IN";
        public const string Out = "OUT";
        public const string Adjustment = "ADJUSTMENT";

        public static readonly IReadOnlyList<string> All = [In, Out, Adjustment];

        public static bool IsValid(string type) =>
            All.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}
