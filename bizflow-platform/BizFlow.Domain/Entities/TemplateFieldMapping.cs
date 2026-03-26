using System;

namespace BizFlow.Domain.Entities;

public partial class TemplateFieldMapping
{
    public int MappingId { get; set; }
    public int TemplateVersionId { get; set; }

    // Field identity
    public string FieldCode { get; set; } = null!;
    public string FieldLabel { get; set; } = null!;

    /// <summary>
    /// auto_increment | date | text | decimal | computed
    /// </summary>
    public string FieldType { get; set; } = null!;

    // Data source (v2: FK references)
    /// <summary>
    /// query | formula | static | auto
    /// </summary>
    public string? SourceType { get; set; }
    public int? SourceEntityId { get; set; }
    public int? SourceFieldId { get; set; }
    public string? FilterJson { get; set; }
    public string? AggregationType { get; set; }

    // Formula
    public long? FormulaId { get; set; }
    public string? FormulaExpression { get; set; }
    public string? DependsOn { get; set; }
    public int? CalculationOrder { get; set; }

    // Export
    public string? ExportColumn { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }

    // Navigation
    public virtual AccountingTemplateVersion TemplateVersion { get; set; } = null!;
    public virtual MappableEntity? SourceEntity { get; set; }
    public virtual MappableField? SourceField { get; set; }
    public virtual FormulaDefinition? Formula { get; set; }
}
