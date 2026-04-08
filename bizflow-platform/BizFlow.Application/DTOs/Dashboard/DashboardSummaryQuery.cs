using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Dashboard;

public class DashboardSummaryQuery
{
    /// <summary>day | week | month | year | custom (case-insensitive)</summary>
    [Required]
    public string Period { get; set; } = string.Empty;

    /// <summary>Omit or null to aggregate across all locations the user can access.</summary>
    [Range(1, int.MaxValue)]
    public int? BusinessLocationId { get; set; }

    /// <summary>Anchor calendar for day/week/month/year presets; defaults to UTC today when omitted.</summary>
    public DateOnly? ReferenceDate { get; set; }

    /// <summary>Required when period is custom.</summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>Required when period is custom.</summary>
    public DateOnly? ToDate { get; set; }
}
