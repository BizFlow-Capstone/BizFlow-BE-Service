namespace BizFlow.Domain.Enums
{
    public static class OrderStatus
    {
        public const string Pending = "pending";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";

        public static readonly IReadOnlyList<string> All = [Pending, Completed, Cancelled];

        public static bool IsValid(string status) =>
            All.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}
