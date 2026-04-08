namespace BizFlow.Domain.Enums
{
    public static class SubscriptionStatus
    {
        public const string Pending = "Pending";
        public const string Active = "Active";
        public const string Expired = "Expired";
        public const string Upgraded = "Upgraded";
        public const string Cancelled = "Cancelled";
        public const string Inactive = "Inactive";

        public static readonly IReadOnlyList<string> All = [Pending, Active, Expired, Upgraded, Cancelled, Inactive];

        public static bool IsValid(string status) =>
            All.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}
