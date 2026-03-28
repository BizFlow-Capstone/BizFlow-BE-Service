using System;

namespace BizFlow.Domain.Entities;

public partial class AccountingExport
{
    public long ExportId { get; set; }
    public long BookId { get; set; }

    public int GroupNumber { get; set; }
    public string? TaxMethod { get; set; }
    public string RulesetVersion { get; set; } = null!;

    public string SummaryJson { get; set; } = null!;
    public int DataRowCount { get; set; }

    /// <summary>
    /// pdf | xlsx
    /// </summary>
    public string ExportFormat { get; set; } = null!;
    public string? FileUrl { get; set; }
    public string? FilePublicId { get; set; }

    public Guid ExportedByUserId { get; set; }
    public DateTime ExportedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual AccountingBook Book { get; set; } = null!;
}
