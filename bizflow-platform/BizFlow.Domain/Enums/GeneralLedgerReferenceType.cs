namespace BizFlow.Domain.Enums
{
    public static class GeneralLedgerReferenceType
    {
        public const string Order = "order";
        public const string Cost = "cost";
        public const string Import = "import";
        public const string DebtorPayment = "debtor_payment";
        public const string Revenue = "revenue";

        public static readonly IReadOnlyList<string> All =
        [
            Order,
            Cost,
            Import,
            DebtorPayment,
            Revenue
        ];

        public static bool IsValid(string type) =>
            All.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}
