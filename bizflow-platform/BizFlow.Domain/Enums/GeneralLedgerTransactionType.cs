namespace BizFlow.Domain.Enums
{
    public static class GeneralLedgerTransactionType
    {
        public const string Sale = "sale";
        public const string ImportCost = "import_cost";
        public const string ManualCost = "manual_cost";
        public const string DebtPayment = "debt_payment";
        public const string ManualRevenue = "manual_revenue";
        public const string ManualExpense = "manual_expense";

        public static readonly IReadOnlyList<string> All =
        [
            Sale,
            ImportCost,
            ManualCost,
            DebtPayment,
            ManualRevenue,
            ManualExpense
        ];

        public static bool IsValid(string type) =>
            All.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}
