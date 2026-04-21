using System.ComponentModel.DataAnnotations;
using BizFlow.Application.DTOs.Reference;

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

    public bool UseSuggestedOpeningBalances { get; set; }
}

public class ReopenAccountingPeriodRequest
{
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = null!;
}

public class CreateCustomAccountingPeriodRequest
{
    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    public decimal? OpeningCashBalance { get; set; }

    public decimal? OpeningBankBalance { get; set; }

    public bool UseSuggestedOpeningBalances { get; set; }
}

public class OpeningBalanceSuggestionRequest
{
    [Required]
    [StringLength(10)]
    public string PeriodType { get; set; } = null!;

    [Range(2000, 9999)]
    public short? Year { get; set; }

    [Range(1, 4)]
    public int? Quarter { get; set; }

    public DateOnly? StartDate { get; set; }
}

public class OpeningBalanceSuggestionDto
{
    public bool HasSuggestion { get; set; }
    public string SuggestionReasonCode { get; set; } = null!;
    public string SuggestionReason { get; set; } = null!;
    public string? CalculationExplanationCode { get; set; }
    public string? CalculationExplanation { get; set; }
    public decimal? OpeningCashBalance { get; set; }
    public decimal? OpeningBankBalance { get; set; }
    public long? SourcePeriodId { get; set; }
    public DateOnly? SourceStartDate { get; set; }
    public DateOnly? SourceEndDate { get; set; }
    public OpeningBalanceCalculationBreakdownDto? CalculationBreakdown { get; set; }
}

public class OpeningBalanceCalculationBreakdownDto
{
    public decimal PreviousOpeningCashBalance { get; set; }
    public decimal PreviousOpeningBankBalance { get; set; }
    public decimal NetCashInSourcePeriod { get; set; }
    public decimal NetBankInSourcePeriod { get; set; }
    public decimal SuggestedOpeningCashBalance { get; set; }
    public decimal SuggestedOpeningBankBalance { get; set; }
}

public class AccountingPeriodDto
{
    public long PeriodId { get; set; }
    public int BusinessLocationId { get; set; }
    /// <summary>
    /// Accounting period type. <c>Code</c> ∈ {<c>quarter</c>, <c>year</c>, <c>custom</c>}.
    /// <c>Label</c> is localized by <c>Accept-Language</c>.
    /// </summary>
    public ReferenceOptionDto PeriodType { get; set; } = null!;
    public short Year { get; set; }
    public int? Quarter { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal? OpeningCashBalance { get; set; }
    public decimal? OpeningBankBalance { get; set; }
    /// <summary>
    /// Accounting period status. <c>Code</c> ∈ {<c>open</c>, <c>finalized</c>, <c>reopened</c>}.
    /// <c>Label</c> is localized by <c>Accept-Language</c>.
    /// </summary>
    public ReferenceOptionDto Status { get; set; } = null!;
    public DateTime? FinalizedAt { get; set; }
    public Guid? FinalizedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AccountingPeriodAuditLogDto
{
    public long LogId { get; set; }
    public long PeriodId { get; set; }
    /// <summary>
    /// Audit log action. <c>Code</c> ∈ {<c>period_created</c>, <c>period_finalized</c>, <c>period_reopened</c>}.
    /// <c>Label</c> is localized by <c>Accept-Language</c>.
    /// </summary>
    public ReferenceOptionDto Action { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}