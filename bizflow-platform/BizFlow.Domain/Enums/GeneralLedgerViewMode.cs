namespace BizFlow.Domain.Enums
{
    public static class GeneralLedgerViewMode
    {
        public const string Audit = "audit";
        public const string Effective = "effective";

        public static readonly IReadOnlyList<string> All =
        [
            Audit,
            Effective
        ];

        public static bool IsValid(string mode) =>
            All.Contains(mode, StringComparer.OrdinalIgnoreCase);
    }
}