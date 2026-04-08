namespace BizFlow.Domain.Entities;

public class TemplateRowDefinition
{
    public int RowDefId { get; set; }
    public int TemplateVersionId { get; set; }

    // Row identity
    public string RowType { get; set; } = null!;
    public string? RowLabel { get; set; }

    // Positioning
    public string Position { get; set; } = "per_group";
    public int SortOrder { get; set; }

    // Grouping
    public string? GroupByField { get; set; }
    public string? SectionType { get; set; }
    public string? SectionFilterValue { get; set; }

    // Data binding
    public string? VisibleFieldCodes { get; set; }
    public long? FormulaId { get; set; }

    // Tax metadata
    public string? TaxType { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual AccountingTemplateVersion TemplateVersion { get; set; } = null!;
    public virtual FormulaDefinition? Formula { get; set; }
}
