using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Accounting;

public class CreateAccountingPeriodRequest
{
    [Required]
    [StringLength(10)]
    public string PeriodType { get; set; } = null!;

    [Range(2000, 9999)]
    public short Year { get; set; }

    [Range(1, 4)]
    public int? Quarter { get; set; }

    public decimal? OpeningCashBalance { get; set; }

    public decimal? OpeningBankBalance { get; set; }
}

public class ReopenAccountingPeriodRequest
{
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = null!;
}

public class AccountingPeriodDto
{
    public long PeriodId { get; set; }
    public int BusinessLocationId { get; set; }
    public string PeriodType { get; set; } = null!;
    public short Year { get; set; }
    public int? Quarter { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal? OpeningCashBalance { get; set; }
    public decimal? OpeningBankBalance { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? FinalizedAt { get; set; }
    public Guid? FinalizedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AccountingPeriodAuditLogDto
{
    public long LogId { get; set; }
    public long PeriodId { get; set; }
    public string Action { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}