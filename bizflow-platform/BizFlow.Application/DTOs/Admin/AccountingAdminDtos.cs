using BizFlow.Domain.Constants;

namespace BizFlow.Application.DTOs.Admin;

public class AdminAccountingOverviewDto
{
    public List<AdminTemplateDto> Templates { get; set; } = new();
    public List<AdminTaxRulesetDto> TaxRulesets { get; set; } = new();
    public List<AdminFormulaDto> Formulas { get; set; } = new();
    public List<AdminBusinessTypeDto> BusinessTypes { get; set; } = new();
}

public class AdminTemplateDto
{
    public int TemplateId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<AdminTemplateVersionDto> Versions { get; set; } = new();
}

public class AdminTemplateVersionDto
{
    public int TemplateVersionId { get; set; }
    public int TemplateId { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public string? ChangeNotes { get; set; }
    public int MappingCount { get; set; }
    public int BookCount { get; set; }
}

public class AdminTemplateVersionDetailDto
{
    public int TemplateVersionId { get; set; }
    public int TemplateId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string VersionLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public string? ChangeNotes { get; set; }
    public List<AdminTemplateFieldMappingDto> FieldMappings { get; set; } = new();
}

public class AdminTemplateFormulaDto
{
    public long FormulaId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FormulaType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> UsedByFieldCodes { get; set; } = new();
}

public class AdminTemplateFieldMappingDto
{
    public int MappingId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public int? SourceEntityId { get; set; }
    public int? SourceFieldId { get; set; }
    public string? FilterJson { get; set; }
    public string? AggregationType { get; set; }
    public long? FormulaId { get; set; }
    public string? FormulaExpression { get; set; }
    public int SortOrder { get; set; }
}

public class AdminTaxRulesetDto
{
    public int RulesetId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int GroupRuleCount { get; set; }
    public int IndustryRateCount { get; set; }
    public int BookCount { get; set; }
}

public class AdminFormulaDto
{
    public long FormulaId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string FormulaType { get; set; } = string.Empty;
    public string ExpressionJson { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AdminBusinessTypeDto
{
    public Guid BusinessTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class UpdateTemplateVersionRequest
{
    public string? VersionLabel { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public string? ChangeNotes { get; set; }
}

public class UpdateTemplateFieldMappingRequest
{
    public string? FieldLabel { get; set; }
    public string? FieldType { get; set; }
    public string? SourceType { get; set; }
    public int? SourceEntityId { get; set; }
    public int? SourceFieldId { get; set; }
    public string? FilterJson { get; set; }
    public string? AggregationType { get; set; }
    public long? FormulaId { get; set; }
    public string? FormulaExpression { get; set; }
    public int? SortOrder { get; set; }
}

public class UpdateFormulaForTestingRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? FormulaType { get; set; }
    public string? ExpressionJson { get; set; }
    public bool? IsActive { get; set; }
}

public class CloneFormulaRequest
{
    public string? NewCode { get; set; }
    public string? NameSuffix { get; set; }
}

public class AdminPreviewRequest
{
    public int BusinessLocationId { get; set; }
    public long PeriodId { get; set; }
    public int TemplateVersionId { get; set; }
    public int GroupNumber { get; set; }
    public string? TaxMethod { get; set; }
    public int RulesetId { get; set; }
    public List<Guid> BusinessTypeIds { get; set; } = new();
    public int BatchSize { get; set; } = 50;
}

public class AdminPreviewResponse
{
    public BookPreviewSummaryDto Summary { get; set; } = new();
    public BookPreviewRowsDto Rows { get; set; } = new();
}

public class BookPreviewSummaryDto
{
    public int TotalRows { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? TotalTax { get; set; }
    public Dictionary<string, decimal>? FormulaValues { get; set; }
}

public class BookPreviewRowsDto
{
    public List<Dictionary<string, object?>> Items { get; set; } = new();
    public bool HasMore { get; set; }
    public string? NextCursor { get; set; }
    public int LoadedCount { get; set; }
    public int? TotalEstimated { get; set; }
}

// ── Reference API ──

public class AdminReferenceDto
{
    public List<AdminEnumValueDto> RowTypes { get; set; } = new();
    public List<AdminEnumValueDto> Positions { get; set; } = new();
    public List<AdminEnumValueDto> SectionTypes { get; set; } = new();
    public List<AdminEnumValueDto> FieldTypes { get; set; } = new();
    public List<AdminEnumValueDto> SourceTypes { get; set; } = new();
    public List<AdminEnumValueDto> TaxTypes { get; set; } = new();
}

public class AdminEnumValueDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

// ── MappableEntity / Field DTOs ──

public class AdminMappableEntityDto
{
    public int EntityId { get; set; }
    public string EntityCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int FieldCount { get; set; }
}

public class AdminMappableEntityDetailDto : AdminMappableEntityDto
{
    public List<AdminMappableFieldDto> Fields { get; set; } = new();
}

public class AdminMappableFieldDto
{
    public int FieldId { get; set; }
    public int EntityId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string AllowedAggregations { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateMappableEntityRequest
{
    public string EntityCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "revenue";
}

public class UpdateMappableEntityRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateMappableFieldRequest
{
    public string FieldCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = "decimal";
    public string AllowedAggregations { get; set; } = "[\"sum\",\"none\"]";
}

public class UpdateMappableFieldRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? DataType { get; set; }
    public string? AllowedAggregations { get; set; }
    public bool? IsActive { get; set; }
}

// ── RowDefinition DTOs ──

public class AdminRowDefinitionDto
{
    public int RowDefId { get; set; }
    public int TemplateVersionId { get; set; }
    public string RowType { get; set; } = string.Empty;
    public string? RowLabel { get; set; }
    public string Position { get; set; } = "per_group";
    public int SortOrder { get; set; }
    public string? GroupByField { get; set; }
    public string? SectionType { get; set; }
    public string? SectionFilterValue { get; set; }
    public string? VisibleFieldCodes { get; set; }
    public long? FormulaId { get; set; }
    public string? FormulaCode { get; set; }
    public string? TaxType { get; set; }
}

public class CreateRowDefinitionRequest
{
    public string RowType { get; set; } = string.Empty;
    public string? RowLabel { get; set; }
    public string Position { get; set; } = "per_group";
    public int SortOrder { get; set; }
    public string? GroupByField { get; set; }
    public string? SectionType { get; set; }
    public string? SectionFilterValue { get; set; }
    public string? VisibleFieldCodes { get; set; }
    public long? FormulaId { get; set; }
    public string? TaxType { get; set; }
}

public class UpdateRowDefinitionRequest
{
    public string? RowType { get; set; }
    public string? RowLabel { get; set; }
    public string? Position { get; set; }
    public int? SortOrder { get; set; }
    public string? GroupByField { get; set; }
    public string? SectionType { get; set; }
    public string? SectionFilterValue { get; set; }
    public string? VisibleFieldCodes { get; set; }
    public long? FormulaId { get; set; }
    public string? TaxType { get; set; }
}

// ── FieldMapping create/delete ──

public class CreateFieldMappingRequest
{
    public string FieldCode { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text";
    public string? SourceType { get; set; }
    public int? SourceEntityId { get; set; }
    public int? SourceFieldId { get; set; }
    public string? FilterJson { get; set; }
    public string? AggregationType { get; set; }
    public long? FormulaId { get; set; }
    public string? FormulaExpression { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
}

// ── Full structure ──

public class AdminFullStructureDto
{
    public int TemplateVersionId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string VersionLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<AdminTemplateFieldMappingDto> FieldMappings { get; set; } = new();
    public List<AdminRowDefinitionDto> RowDefinitions { get; set; } = new();
    public string RenderPreview { get; set; } = string.Empty;
}
