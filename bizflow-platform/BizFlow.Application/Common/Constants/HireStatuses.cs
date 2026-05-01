namespace BizFlow.Application.Common.Constants;

public static class HireStatuses
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";

    public static readonly IReadOnlyList<string> All = [Pending, Accepted, Rejected];
}
