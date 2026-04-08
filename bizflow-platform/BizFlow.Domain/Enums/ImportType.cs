namespace BizFlow.Domain.Enums
{
    /// <summary>
    /// Import type constants (for reference only, entity uses string)
    /// </summary>
    public static class ImportType
    {
        public const string Invoice = "INVOICE";
        public const string InventoryAdjustment = "INVENTORY_ADJUSTMENT";
        public const string Return = "RETURN";

        public static readonly IReadOnlyList<string> All = [Invoice, InventoryAdjustment, Return];

        public static bool IsValid(string type) =>
            All.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}
