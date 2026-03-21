namespace BizFlow.Domain.Enums
{
    /// <summary>
    /// Revenue type constants.
    /// </summary>
    public static class RevenueType
    {
        public const string Sale = "sale";
        public const string Manual = "manual";

        public static readonly IReadOnlyList<string> All = [Sale, Manual];

        public static bool IsValid(string revenueType) =>
            All.Contains(revenueType, StringComparer.OrdinalIgnoreCase);
    }
}
