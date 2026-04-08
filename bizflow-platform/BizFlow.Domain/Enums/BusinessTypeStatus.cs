namespace BizFlow.Domain.Enums
{
    /// <summary>
    /// Business type status constants (for reference only, entity uses string)
    /// </summary>
    public static class BusinessTypeStatus
    {
        public const string Active = "active";
        public const string Inactive = "inactive";

        public static readonly IReadOnlyList<string> All = [Active, Inactive];

        public static bool IsValid(string status) =>
            All.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}
