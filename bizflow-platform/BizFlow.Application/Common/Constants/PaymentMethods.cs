namespace BizFlow.Application.Common.Constants
{
    /// <summary>
    /// Centralized list of supported payment methods.
    /// Add new methods here — do NOT hardcode in services.
    /// </summary>
    public static class PaymentMethods
    {
        public const string Cash = "cash";
        public const string Bank = "bank";

        public static readonly IReadOnlyList<string> All = [Cash, Bank];

        public static bool IsValid(string method) =>
            All.Contains(method, StringComparer.OrdinalIgnoreCase);
    }
}
