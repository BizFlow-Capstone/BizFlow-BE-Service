namespace BizFlow.Application.Common.Models
{
    public enum FeatureAccessDenialReason
    {
        None = 0,
        InvalidRequest,
        NoLocationAccess,
        OwnerNotResolved,
        NoActiveSubscription,
        FeatureNotInPlan,
        UsageLimitReached
    }

    public sealed class FeatureAccessEvaluationResult
    {
        public bool Allowed { get; init; }
        public FeatureAccessDenialReason DenialReason { get; init; }

        public int? Used { get; init; }

        public int? Limit { get; init; }

        public static FeatureAccessEvaluationResult Ok() =>
            new() { Allowed = true, DenialReason = FeatureAccessDenialReason.None };

        public static FeatureAccessEvaluationResult Deny(
            FeatureAccessDenialReason reason,
            int? used = null,
            int? limit = null) =>
            new()
            {
                Allowed = false,
                DenialReason = reason,
                Used = used,
                Limit = limit
            };
    }
}

