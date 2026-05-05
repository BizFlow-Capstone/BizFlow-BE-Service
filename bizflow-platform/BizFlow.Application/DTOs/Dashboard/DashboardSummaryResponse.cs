namespace BizFlow.Application.DTOs.Dashboard;

public class DashboardSummaryResponse
{
    /// <summary>Set when a single location was requested; null when totals span all accessible locations.</summary>
    public int? BusinessLocationId { get; set; }

    /// <summary>Number of locations included in the aggregates (0 if none).</summary>
    public int IncludedLocationCount { get; set; }

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public int TotalCompletedOrders { get; set; }

    /// <summary>
    /// Net change in debtor balance (sum of BalanceAfter − BalanceBefore) for all debt transactions
    /// with <c>PaidAt</c> in <see cref="FromDate"/>–<see cref="ToDate"/> (inclusive, UTC day bounds). Positive = more owed to the business.
    /// </summary>
    public decimal OutstandingDebtNetChangeInPeriod { get; set; }
}
