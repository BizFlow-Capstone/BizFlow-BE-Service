namespace BizFlow.Application.Common.Constants;

public static class AccountingPeriodConstants
{
    public static class PeriodTypes
    {
        public const string Quarter = "quarter";
        public const string Year = "year";
        public const string Custom = "custom";
    }

    public static class PeriodStatuses
    {
        public const string Open = "open";
        public const string Finalized = "finalized";
        public const string Reopened = "reopened";
    }

    public static class AuditActions
    {
        public const string PeriodCreated = "period_created";
        public const string PeriodFinalized = "period_finalized";
        public const string PeriodReopened = "period_reopened";
    }
}