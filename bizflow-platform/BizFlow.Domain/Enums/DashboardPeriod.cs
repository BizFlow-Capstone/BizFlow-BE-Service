namespace BizFlow.Domain.Enums
{
    /// <summary>
    /// Dashboard date-range preset for user KPI aggregates (API query: period=...).
    /// </summary>
    public static class DashboardPeriod
    {
        public const string Day = "day";
        public const string Week = "week";
        public const string Month = "month";
        public const string Year = "year";
        public const string Custom = "custom";

        public static readonly IReadOnlyList<string> All = [Day, Week, Month, Year, Custom];

        public static bool IsValid(string? value) =>
            value != null && All.Contains(value, StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns canonical lowercase token, or null if not in <see cref="All"/>.</summary>
        public static string? NormalizeOrNull(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var t = raw.Trim();
            foreach (var a in All)
            {
                if (a.Equals(t, StringComparison.OrdinalIgnoreCase))
                    return a;
            }

            return null;
        }
    }
}
