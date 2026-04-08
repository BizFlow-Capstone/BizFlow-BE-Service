namespace BizFlow.Domain.Enums
{
    /// <summary>
    /// Import status constants (for reference only, entity uses string)
    /// </summary>
    public static class ImportStatus
    {
        public const string Draft = "DRAFT";
        public const string Confirmed = "CONFIRMED";
        public const string Cancelled = "CANCELLED";

        public static readonly IReadOnlyList<string> All = [Draft, Confirmed, Cancelled];

        public static bool IsValid(string status) =>
            All.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}
