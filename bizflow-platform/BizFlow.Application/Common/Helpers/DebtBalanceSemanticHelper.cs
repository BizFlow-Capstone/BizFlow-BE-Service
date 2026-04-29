using BizFlow.Application.Common.Constants;

namespace BizFlow.Application.Common.Helpers
{
    public static class DebtBalanceSemanticHelper
    {
        public static decimal ApplyOrderDebtIncrease(decimal currentBalance, decimal debtAmount) =>
            currentBalance + debtAmount;

        public static decimal ApplyOrderDebtRollback(decimal currentBalance, decimal debtAmount) =>
            currentBalance - debtAmount;

        public static decimal ApplyUserDebtAction(decimal currentBalance, decimal amount, string action)
        {
            var normalized = action.Trim().ToLowerInvariant();
            return normalized switch
            {
                DebtPaymentActions.DecreaseDebt => currentBalance - amount,
                DebtPaymentActions.IncreaseDebt => currentBalance + amount,
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported debt action")
            };
        }

        public static decimal CalculateOutstandingDebt(decimal currentBalance) =>
            Math.Max(0m, currentBalance);

        public static string ResolveDebtDirection(decimal balanceBefore, decimal balanceAfter)
        {
            var delta = balanceAfter - balanceBefore;
            if (delta == 0m)
                return DebtDirection.Neutral;

            return delta > 0 ? DebtDirection.Increase : DebtDirection.Decrease;
        }
    }
}
