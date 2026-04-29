namespace BizFlow.Application.Common.Constants
{
    public static class DebtPaymentActions
    {
        public const string DecreaseDebt = "decrease_debt";
        public const string IncreaseDebt = "increase_debt";
        public const string SystemRollback = "system_rollback";

        public static readonly IReadOnlyList<string> UserActions = [DecreaseDebt, IncreaseDebt];
        public static readonly IReadOnlyList<string> All = [DecreaseDebt, IncreaseDebt, SystemRollback];

        public static bool IsValidUserAction(string action) =>
            UserActions.Contains(action, StringComparer.OrdinalIgnoreCase);
    }
}
