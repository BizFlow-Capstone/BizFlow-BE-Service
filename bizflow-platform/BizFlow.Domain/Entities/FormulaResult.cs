using System;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Cache computation results — vertical layout (1 row = 1 result).
/// Uses empty string instead of NULL for context keys so UNIQUE INDEX works correctly.
/// </summary>
public partial class FormulaResult
{
    public long ResultId { get; set; }

    public long BookId { get; set; }
    public long FormulaId { get; set; }

    // Context scope
    public string ProductId { get; set; } = "";
    public string BusinessTypeId { get; set; } = "";
    public string SectionCode { get; set; } = "";

    // Result
    public decimal ResultValue { get; set; }

    // Cache management
    public DateTime ComputedAt { get; set; }
    public bool IsStale { get; set; }

    // Navigation
    public virtual AccountingBook Book { get; set; } = null!;
    public virtual FormulaDefinition Formula { get; set; } = null!;
}
