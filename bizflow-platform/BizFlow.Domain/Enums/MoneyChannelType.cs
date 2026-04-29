namespace BizFlow.Domain.Enums
{
    public static class MoneyChannelType
    {
        public const string Cash = "cash";
        public const string Bank = "bank";
        public const string Debt = "debt";
        public const string System = "system";

        public static readonly IReadOnlyList<string> All = [Cash, Bank, Debt, System];
        public static readonly IReadOnlyList<string> ExposedToUser = [Cash, Bank, Debt];

        public static bool IsValid(string type) =>
            ExposedToUser.Contains(type, StringComparer.OrdinalIgnoreCase);

        public static bool IsValidInternal(string type) =>
            All.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}
