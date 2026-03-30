namespace BizFlow.Domain.Enums
{
    public static class SubscriptionAuditAction
    {
        public const string Activated = "ACTIVATED";
        public const string Expired = "EXPIRED";
        public const string Renewed = "RENEWED";
        public const string UsageSnapshot = "USAGE_SNAPSHOT";
        public const string FirestoreResync = "FIRESTORE_RESYNC";
        public const string FirestoreAnomaly = "FIRESTORE_ANOMALY";

        public static readonly IReadOnlyList<string> All =
            [Activated, Expired, Renewed, UsageSnapshot, FirestoreResync, FirestoreAnomaly];
    }
}
