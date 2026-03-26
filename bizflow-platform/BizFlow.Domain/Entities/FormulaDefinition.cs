using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class FormulaDefinition
{
    public long FormulaId { get; set; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>
    /// AGGREGATE | CELL_REF | TAX_RATE | WEIGHTED_AVG | EXTERNAL_LOOKUP
    /// </summary>
    public string FormulaType { get; set; } = null!;

    /// <summary>
    /// JSON AST expression — see tax-formular-engine.md Section 4
    /// </summary>
    public string ExpressionJson { get; set; } = null!;

    /// <summary>
    /// decimal | integer
    /// </summary>
    public string ResultDataType { get; set; } = "decimal";

    /// <summary>
    /// floor | ceil | round_half_up | null
    /// </summary>
    public string? RoundingMode { get; set; }
    public int RoundingPrecision { get; set; }

    public bool IsActive { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<FormulaResult> Results { get; set; } = new List<FormulaResult>();
    public virtual ICollection<TemplateFieldMapping> FieldMappings { get; set; } = new List<TemplateFieldMapping>();
}
