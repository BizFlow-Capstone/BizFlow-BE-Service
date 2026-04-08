namespace BizFlow.Domain.Enums
{
    public static class MoneyChannelType
    {
        public const string Cash = "cash";
        public const string Bank = "bank";
        public const string Debt = "debt";

        public static readonly IReadOnlyList<string> All = [Cash, Bank, Debt];

        public static bool IsValid(string type) =>
            All.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}
