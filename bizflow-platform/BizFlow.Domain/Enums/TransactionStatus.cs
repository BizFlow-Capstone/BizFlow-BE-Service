namespace BizFlow.Domain.Enums
{
    public static class TransactionStatus
    {
        public const string Pending = "Pending";
        public const string Success = "Success";
        public const string Failed = "Failed";
        public const string Refunded = "Refunded";

        public static readonly IReadOnlyList<string> All = [Pending, Success, Failed, Refunded];

        public static bool IsValid(string status) =>
            All.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}
