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

    /// <summary>Total amount customers still owe (sum of -CurrentBalance for negative balances). Snapshot at query time.</summary>
    public decimal TotalOutstandingDebt { get; set; }

    /// <summary>UTC timestamp when outstanding debt was computed.</summary>
    public DateTime OutstandingDebtAsOfUtc { get; set; }
}
