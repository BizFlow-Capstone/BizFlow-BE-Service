namespace BizFlow.Application.Common.Constants
{
    /// <summary>
    /// Centralized list of supported payment methods.
    /// </summary>
    public static class PaymentMethods
    {
        public const string Cash = "cash";
        public const string Bank = "bank";

        public const string System = "system";

        public static readonly IReadOnlyList<string> All = [Cash, Bank, System];

        public static readonly IReadOnlyList<string> ExposedToUser = [Cash, Bank];

        public static bool IsValid(string method) =>
            ExposedToUser.Contains(method, StringComparer.OrdinalIgnoreCase);
            
        public static bool IsValidInternal(string method) =>
            All.Contains(method, StringComparer.OrdinalIgnoreCase);
    }
}
