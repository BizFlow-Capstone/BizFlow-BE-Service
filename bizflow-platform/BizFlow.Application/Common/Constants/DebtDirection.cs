namespace BizFlow.Application.Common.Constants
{
    public static class DebtDirection
    {
        public const string Increase = "debt_increase";
        public const string Decrease = "debt_decrease";
        public const string Neutral = "neutral";

        public static readonly IReadOnlyList<string> All = [Increase, Decrease, Neutral];
    }
}
