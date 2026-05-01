namespace BizFlow.Application.Common.Constants;

public static class LedgerEffectiveStatuses
{
    public const string Active = "active";
    public const string Reversed = "reversed";
    public const string Reversal = "reversal";

    public static readonly IReadOnlyList<string> All = [Active, Reversed, Reversal];
}
