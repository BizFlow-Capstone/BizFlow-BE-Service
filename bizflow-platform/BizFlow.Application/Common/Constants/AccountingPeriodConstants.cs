namespace BizFlow.Application.Common.Constants;

public static class AccountingPeriodConstants
{
    public static class PeriodTypes
    {
        public const string Quarter = "quarter";
        public const string Year = "year";
        public const string Custom = "custom";

        public static readonly IReadOnlyList<string> All = [Quarter, Year, Custom];
    }

    public static class PeriodStatuses
    {
        public const string Open = "open";
        public const string Finalized = "finalized";
        public const string Reopened = "reopened";

        public static readonly IReadOnlyList<string> All = [Open, Finalized, Reopened];
    }

    public static class AuditActions
    {
        public const string PeriodCreated = "period_created";
        public const string PeriodFinalized = "period_finalized";
        public const string PeriodReopened = "period_reopened";

        public static readonly IReadOnlyList<string> All = [PeriodCreated, PeriodFinalized, PeriodReopened];
    }
}