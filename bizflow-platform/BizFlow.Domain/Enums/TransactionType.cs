namespace BizFlow.Domain.Enums
{
    public static class TransactionType
    {
        public const string Purchase = "PURCHASE";
        public const string Renew = "RENEW";

        public static readonly IReadOnlyList<string> All = [Purchase, Renew];

        public static bool IsValid(string type) =>
            All.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}
