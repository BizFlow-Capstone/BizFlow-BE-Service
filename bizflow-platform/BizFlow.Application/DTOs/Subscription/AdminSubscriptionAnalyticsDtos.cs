using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Subscription;

public class AdminSubscriptionAnalyticsQuery
{
    /// <summary>day | week | month | year | custom (case-insensitive). Default: week.</summary>
    public string? Period { get; set; } = "week";

    /// <summary>Anchor calendar for day/week/month/year presets; defaults to UTC today when omitted.</summary>
    public DateOnly? ReferenceDate { get; set; }

    /// <summary>Required when period is custom.</summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>Required when period is custom.</summary>
    public DateOnly? ToDate { get; set; }
}

public class AdminSubscriptionAnalyticsResponse
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalSubscriptionRegistrations { get; set; }
    public List<AdminSubscriptionAnalyticsDailyPoint> DailySeries { get; set; } = [];
}

public class AdminSubscriptionAnalyticsDailyPoint
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int SubscriptionRegistrations { get; set; }
}

public class SubscriptionTransactionDailyAggregate
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    [Range(0, int.MaxValue)]
    public int SubscriptionRegistrations { get; set; }
}

public class SubscriptionTransactionAnalyticsRecord
{
    public int SubscriptionPlanId { get; set; }
    public decimal PlanPrice { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime PaidAtUtc { get; set; }
}
