namespace BizFlow.Domain.Enums
{
    /// <summary>
    /// Revenue lifecycle status.
    /// <para><b>draft</b> — created but not yet posted to GL. Editable in-place.</para>
    /// <para><b>posted</b> — revenue is live, has produced GL entries. Edits must go through the replacement flow.</para>
    /// <para><b>cancelled</b> — explicitly cancelled (standalone cancel). GL entries have been reversed.</para>
    /// <para><b>replaced</b> — cancelled as part of the replacement flow; a newer Revenue row supersedes it.</para>
    /// </summary>
    public static class RevenueStatus
    {
        public const string Draft = "draft";
        public const string Posted = "posted";
        public const string Cancelled = "cancelled";
        public const string Replaced = "replaced";

        public static readonly IReadOnlyList<string> All = [Draft, Posted, Cancelled, Replaced];

        public static bool IsValid(string status) =>
            All.Contains(status, StringComparer.OrdinalIgnoreCase);

        public static bool IsEditableInPlace(string status) =>
            string.Equals(status, Draft, StringComparison.OrdinalIgnoreCase);

        public static bool IsTerminal(string status) =>
            string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, Replaced, StringComparison.OrdinalIgnoreCase);
    }
}
