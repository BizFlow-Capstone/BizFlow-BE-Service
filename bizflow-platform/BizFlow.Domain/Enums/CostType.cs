namespace BizFlow.Domain.Enums
{
    /// <summary>
    /// Cost type constants.
    /// </summary>
    public static class CostType
    {
        public const string Import = "import";
        public const string Salary = "salary";
        public const string Rent = "rent";
        public const string Utilities = "utilities";
        public const string Transport = "transport";
        public const string Marketing = "marketing";
        public const string Maintenance = "maintenance";
        public const string Depreciation = "depreciation";
        public const string Interest = "interest";
        public const string Other = "other";
        public const string Manual = "manual";

        public static readonly IReadOnlyList<string> All = [Import, Salary, Rent, Utilities, Transport, Marketing, Maintenance, Depreciation, Interest, Other, Manual];

        public static bool IsValid(string costType) =>
            All.Contains(costType, StringComparer.OrdinalIgnoreCase);
    }
}
