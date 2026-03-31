using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Services;

public class AdminAccountingService : IAdminAccountingService
{
    private const int TemplateVersionLabelMaxLength = 50;

    private readonly IUnitOfWork _uow;
    private readonly IBookRenderingService _renderingService;

    public AdminAccountingService(IUnitOfWork uow, IBookRenderingService renderingService)
    {
        _uow = uow;
        _renderingService = renderingService;
    }

    public async Task<AdminAccountingOverviewDto> GetOverviewAsync()
    {
        var templates = await _uow.AccountingTemplates.GetAllWithVersionsAsync();
        var rulesets = await _uow.TaxRulesets.GetAllWithRulesAsync();
        var formulas = await _uow.FormulaDefinitions.GetAllAsync();
        var businessTypes = await _uow.BusinessTypes.GetAllAsync();

        return new AdminAccountingOverviewDto
        {
            Templates = templates.Select(t => new AdminTemplateDto
            {
                TemplateId = t.TemplateId,
                TemplateCode = t.TemplateCode,
                Name = t.Name,
                IsActive = t.IsActive,
                Versions = t.Versions
                    .OrderByDescending(v => v.CreatedAt)
                    .Select(MapTemplateVersion)
                    .ToList()
            }).ToList(),
            TaxRulesets = rulesets.Select(MapTaxRuleset).ToList(),
            Formulas = formulas.Select(MapFormula).ToList(),
            BusinessTypes = businessTypes.Select(bt => new AdminBusinessTypeDto
            {
                BusinessTypeId = bt.BusinessTypeId,
                Code = bt.Code,
                Name = bt.Name,
                Status = bt.Status
            }).ToList()
        };
    }

    public async Task<AdminTemplateVersionDetailDto> GetTemplateVersionDetailAsync(int templateVersionId)
    {
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        return new AdminTemplateVersionDetailDto
        {
            TemplateVersionId = version.TemplateVersionId,
            TemplateId = version.TemplateId,
            TemplateCode = version.Template?.TemplateCode ?? string.Empty,
            TemplateName = version.Template?.Name ?? string.Empty,
            VersionLabel = version.VersionLabel,
            IsActive = version.IsActive,
            EffectiveFrom = version.EffectiveFrom,
            ChangeNotes = version.ChangeNotes,
            FieldMappings = version.FieldMappings
                .OrderBy(m => m.SortOrder)
                .Select(m => new AdminTemplateFieldMappingDto
                {
                    MappingId = m.MappingId,
                    FieldCode = m.FieldCode,
                    FieldLabel = m.FieldLabel,
                    FieldType = m.FieldType,
                    SourceType = m.SourceType,
                    SourceEntityId = m.SourceEntityId,
                    SourceFieldId = m.SourceFieldId,
                    FilterJson = m.FilterJson,
                    AggregationType = m.AggregationType,
                    FormulaId = m.FormulaId,
                    FormulaExpression = m.FormulaExpression,
                    SortOrder = m.SortOrder
                }).ToList()
        };
    }

        public async Task<List<AdminTemplateFormulaDto>> GetTemplateFormulasAsync(int templateVersionId)
        {
            var version = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
                ?? throw new NotFoundException(MessageKeys.NotFound);

            return version.FieldMappings
                .Where(m => m.FormulaId.HasValue && m.Formula != null)
                .GroupBy(m => new
                {
                    FormulaId = m.FormulaId!.Value,
                    m.Formula!.Code,
                    m.Formula.Name,
                    m.Formula.FormulaType,
                    m.Formula.IsActive
                })
                .Select(g => new AdminTemplateFormulaDto
                {
                    FormulaId = g.Key.FormulaId,
                    Code = g.Key.Code,
                    Name = g.Key.Name,
                    FormulaType = g.Key.FormulaType,
                    IsActive = g.Key.IsActive,
                    UsedByFieldCodes = g
                        .Select(x => x.FieldCode)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList()
                })
                .OrderBy(x => x.Code)
                .ToList();
        }

    public async Task<AdminTemplateVersionDto> CloneTemplateVersionAsync(int templateVersionId, Guid actorUserId)
    {
        var source = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var clone = new AccountingTemplateVersion
        {
            TemplateId = source.TemplateId,
            VersionLabel = BuildDraftVersionLabel(source.VersionLabel),
            IsActive = false,
            EffectiveFrom = source.EffectiveFrom,
            TemplateFileUrl = source.TemplateFileUrl,
            ChangeNotes = source.ChangeNotes,
            CreatedByUserId = actorUserId,
            CreatedAt = DateTime.UtcNow,
            FieldMappings = source.FieldMappings
                .OrderBy(m => m.SortOrder)
                .Select(m => new TemplateFieldMapping
                {
                    FieldCode = m.FieldCode,
                    FieldLabel = m.FieldLabel,
                    FieldType = m.FieldType,
                    SourceType = m.SourceType,
                    SourceEntityId = m.SourceEntityId,
                    SourceFieldId = m.SourceFieldId,
                    FilterJson = m.FilterJson,
                    AggregationType = m.AggregationType,
                    FormulaId = m.FormulaId,
                    FormulaExpression = m.FormulaExpression,
                    DependsOn = m.DependsOn,
                    CalculationOrder = m.CalculationOrder,
                    ExportColumn = m.ExportColumn,
                    SortOrder = m.SortOrder,
                    IsRequired = m.IsRequired
                }).ToList(),
            RowDefinitions = source.RowDefinitions
                .OrderBy(r => r.SortOrder)
                .Select(r => new TemplateRowDefinition
                {
                    RowType = r.RowType,
                    RowLabel = r.RowLabel,
                    Position = r.Position,
                    SortOrder = r.SortOrder,
                    GroupByField = r.GroupByField,
                    SectionType = r.SectionType,
                    SectionFilterValue = r.SectionFilterValue,
                    VisibleFieldCodes = r.VisibleFieldCodes,
                    FormulaId = r.FormulaId,
                    TaxType = r.TaxType,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
        };

        await _uow.AccountingTemplates.AddVersionAsync(clone);
        await _uow.SaveChangesAsync();

        return MapTemplateVersion(clone);
    }

    public async Task<AdminTemplateVersionDto> UpdateTemplateVersionAsync(int templateVersionId, UpdateTemplateVersionRequest request)
    {
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (request.VersionLabel != null)
        {
            var label = request.VersionLabel.Trim();
            if (label.Length > TemplateVersionLabelMaxLength)
            {
                throw new BadRequestException(
                    MessageKeys.BadRequest,
                    $"VersionLabel must be <= {TemplateVersionLabelMaxLength} characters");
            }

            version.VersionLabel = label;
        }

        if (request.EffectiveFrom.HasValue)
            version.EffectiveFrom = request.EffectiveFrom;

        if (request.ChangeNotes != null)
            version.ChangeNotes = request.ChangeNotes;

        _uow.AccountingTemplates.UpdateVersion(version);
        await _uow.SaveChangesAsync();

        return MapTemplateVersion(version);
    }

    public async Task<AdminTemplateVersionDto> ActivateTemplateVersionAsync(int templateVersionId)
    {
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var allTemplates = await _uow.AccountingTemplates.GetAllWithVersionsAsync();
        var template = allTemplates.FirstOrDefault(t => t.TemplateId == version.TemplateId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        foreach (var v in template.Versions)
            v.IsActive = v.TemplateVersionId == templateVersionId;

        template.IsActive = true;

        foreach (var v in template.Versions)
            _uow.AccountingTemplates.UpdateVersion(v);

        await _uow.SaveChangesAsync();

        return MapTemplateVersion(template.Versions.First(v => v.TemplateVersionId == templateVersionId));
    }

    public async Task<AdminTemplateVersionDto> DeactivateTemplateVersionAsync(int templateVersionId)
    {
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (version.IsActive)
        {
            var templates = await _uow.AccountingTemplates.GetAllWithVersionsAsync();
            var siblings = templates
                .Where(t => t.TemplateId == version.TemplateId)
                .SelectMany(t => t.Versions)
                .Where(v => v.TemplateVersionId != templateVersionId)
                .ToList();

            if (!siblings.Any(v => v.IsActive))
                throw new BadRequestException(MessageKeys.BadRequest,
                    "Cannot deactivate the only active template version. Activate another version first.");
        }

        version.IsActive = false;
        _uow.AccountingTemplates.UpdateVersion(version);
        await _uow.SaveChangesAsync();

        return MapTemplateVersion(version);
    }

    public async Task DeleteTemplateVersionDraftAsync(int templateVersionId)
    {
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (version.IsActive)
            throw new BadRequestException(MessageKeys.BadRequest, "Cannot delete active version");

        if (version.AccountingBooks.Any())
            throw new BadRequestException(MessageKeys.BadRequest, "Only draft version can be deleted");

        _uow.AccountingTemplates.RemoveVersion(version);
        await _uow.SaveChangesAsync();
    }

    public async Task<AdminTemplateFieldMappingDto> UpdateTemplateFieldMappingForTestingAsync(int mappingId, UpdateTemplateFieldMappingRequest request)
    {
        var mapping = await _uow.AccountingTemplates.GetMappingByIdAsync(mappingId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (request.FieldLabel != null)
            mapping.FieldLabel = request.FieldLabel;
        if (request.FieldType != null)
            mapping.FieldType = request.FieldType;
        if (request.SourceType != null)
            mapping.SourceType = request.SourceType;
        if (request.SourceEntityId.HasValue)
            mapping.SourceEntityId = request.SourceEntityId;
        if (request.SourceFieldId.HasValue)
            mapping.SourceFieldId = request.SourceFieldId;
        if (request.FilterJson != null)
            mapping.FilterJson = request.FilterJson;
        if (request.AggregationType != null)
            mapping.AggregationType = request.AggregationType;
        if (request.FormulaId.HasValue)
            mapping.FormulaId = request.FormulaId;
        if (request.FormulaExpression != null)
            mapping.FormulaExpression = request.FormulaExpression;
        if (request.SortOrder.HasValue)
            mapping.SortOrder = request.SortOrder.Value;

        _uow.AccountingTemplates.UpdateMapping(mapping);
        await _uow.SaveChangesAsync();

        return new AdminTemplateFieldMappingDto
        {
            MappingId = mapping.MappingId,
            FieldCode = mapping.FieldCode,
            FieldLabel = mapping.FieldLabel,
            FieldType = mapping.FieldType,
            SourceType = mapping.SourceType,
            SourceEntityId = mapping.SourceEntityId,
            SourceFieldId = mapping.SourceFieldId,
            FilterJson = mapping.FilterJson,
            AggregationType = mapping.AggregationType,
            FormulaId = mapping.FormulaId,
            FormulaExpression = mapping.FormulaExpression,
            SortOrder = mapping.SortOrder
        };
    }

    public async Task<AdminFormulaDto> GetFormulaDetailAsync(long formulaId)
    {
        var formula = await _uow.FormulaDefinitions.GetByIdAsync(formulaId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        return MapFormula(formula);
    }

    public async Task<AdminFormulaDto> UpdateFormulaForTestingAsync(long formulaId, UpdateFormulaForTestingRequest request)
    {
        var formula = await _uow.FormulaDefinitions.GetByIdAsync(formulaId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (request.Name != null)
            formula.Name = request.Name;
        if (request.Description != null)
            formula.Description = request.Description;
        if (request.FormulaType != null)
            formula.FormulaType = request.FormulaType;
        if (request.ExpressionJson != null)
            formula.ExpressionJson = request.ExpressionJson;
        if (request.IsActive.HasValue)
            formula.IsActive = request.IsActive.Value;

        formula.UpdatedAt = DateTime.UtcNow;

        _uow.FormulaDefinitions.Update(formula);
        await _uow.SaveChangesAsync();

        return MapFormula(formula);
    }

    public async Task<AdminFormulaDto> CloneFormulaAsync(long formulaId, CloneFormulaRequest request)
    {
        var source = await _uow.FormulaDefinitions.GetByIdAsync(formulaId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var code = string.IsNullOrWhiteSpace(request.NewCode)
            ? $"{source.Code}_DRAFT_{DateTime.UtcNow:yyyyMMddHHmmss}"
            : request.NewCode.Trim();

        var clone = new FormulaDefinition
        {
            Code = code,
            Name = string.IsNullOrWhiteSpace(request.NameSuffix) ? source.Name : $"{source.Name} {request.NameSuffix}",
            Description = source.Description,
            FormulaType = source.FormulaType,
            ExpressionJson = source.ExpressionJson,
            ResultDataType = source.ResultDataType,
            RoundingMode = source.RoundingMode,
            RoundingPrecision = source.RoundingPrecision,
            IsActive = false,
            CreatedByUserId = source.CreatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.FormulaDefinitions.AddAsync(clone);
        await _uow.SaveChangesAsync();

        return MapFormula(clone);
    }

    public async Task<AdminTaxRulesetDto> ActivateTaxRulesetAsync(int rulesetId)
    {
        var rulesets = await _uow.TaxRulesets.GetAllWithRulesAsync();
        var selected = rulesets.FirstOrDefault(x => x.RulesetId == rulesetId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        foreach (var rs in rulesets)
        {
            rs.IsActive = rs.RulesetId == rulesetId;
            _uow.TaxRulesets.Update(rs);
        }

        await _uow.SaveChangesAsync();

        return MapTaxRuleset(selected);
    }

    public async Task<AdminTaxRulesetDto> DeactivateTaxRulesetAsync(int rulesetId)
    {
        var ruleset = await _uow.TaxRulesets.GetByIdWithRulesAsync(rulesetId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (ruleset.IsActive)
        {
            var allRulesets = await _uow.TaxRulesets.GetAllWithRulesAsync();
            if (!allRulesets.Any(x => x.RulesetId != rulesetId && x.IsActive))
                throw new BadRequestException(MessageKeys.BadRequest,
                    "Cannot deactivate the only active tax ruleset. Activate another ruleset first.");
        }

        ruleset.IsActive = false;
        _uow.TaxRulesets.Update(ruleset);
        await _uow.SaveChangesAsync();

        return MapTaxRuleset(ruleset);
    }

    public async Task<AdminPreviewResponse> PreviewAsync(AdminPreviewRequest request)
    {
        if (request.BusinessLocationId <= 0)
            throw new BadRequestException(MessageKeys.BadRequest, "BusinessLocationId must be greater than 0");

        if (request.PeriodId <= 0)
            throw new BadRequestException(MessageKeys.BadRequest, "PeriodId must be greater than 0");

        if (request.TemplateVersionId <= 0)
            throw new BadRequestException(MessageKeys.BadRequest, "TemplateVersionId must be greater than 0");

        if (request.RulesetId <= 0)
            throw new BadRequestException(MessageKeys.BadRequest, "RulesetId must be greater than 0");

        if (request.BatchSize < 1)
            request.BatchSize = 1;
        if (request.BatchSize > 200)
            request.BatchSize = 200;

        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAsync(request.TemplateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound, $"TemplateVersionId={request.TemplateVersionId}");

        var period = await _uow.AccountingPeriods.GetByLocationAndIdAsync(request.BusinessLocationId, request.PeriodId)
            ?? throw new NotFoundException(
                MessageKeys.PeriodNotFound,
                $"LocationId={request.BusinessLocationId}",
                $"PeriodId={request.PeriodId}");

        var ruleset = await _uow.TaxRulesets.GetByIdWithRulesAsync(request.RulesetId)
            ?? throw new NotFoundException(MessageKeys.NotFound, $"RulesetId={request.RulesetId}");

        var businessTypeIds = request.BusinessTypeIds.Distinct().ToList();
        if (businessTypeIds.Count == 0)
        {
            var products = await _uow.Products.QuickSearchByLocationAsync(request.BusinessLocationId, null);
            businessTypeIds = products.Where(p => p.DeletedAt == null).Select(p => p.BusinessTypeId).Distinct().ToList();

            if (businessTypeIds.Count == 0)
            {
                var revenueQuery = new RevenueQueryParams
                {
                    BusinessLocationId = request.BusinessLocationId,
                    FromDate = period.StartDate,
                    ToDate = period.EndDate,
                    PageNumber = 1,
                    PageSize = int.MaxValue
                };
                var (revenues, _) = await _uow.Revenues.SearchAsync(revenueQuery);
                businessTypeIds = revenues
                    .Where(r => r.DeletedAt == null && r.BusinessTypeId.HasValue)
                    .Select(r => r.BusinessTypeId!.Value)
                    .Distinct()
                    .ToList();
            }
        }

        var ctx = new BookRenderContext
        {
            BookId = 0,
            BusinessLocationId = request.BusinessLocationId,
            PeriodId = request.PeriodId,
            PeriodStart = period.StartDate,
            PeriodEnd = period.EndDate,
            TemplateVersionId = request.TemplateVersionId,
            TemplateCode = version.Template?.TemplateCode ?? string.Empty,
            GroupNumber = request.GroupNumber,
            TaxMethod = request.TaxMethod,
            RulesetId = ruleset.RulesetId,
            BusinessTypeIds = businessTypeIds
        };

        var summary = await _renderingService.ComputeSummaryAsync(ctx);
        var rows = await _renderingService.RenderRowsAsync(ctx, null, request.BatchSize);

        return new AdminPreviewResponse
        {
            Summary = new BookPreviewSummaryDto
            {
                TotalRows = summary.TotalRows,
                TotalRevenue = summary.TotalRevenue,
                TotalCost = summary.TotalCost,
                TotalTax = summary.TotalTax,
                FormulaValues = summary.FormulaValues
            },
            Rows = new BookPreviewRowsDto
            {
                Items = rows.Rows,
                HasMore = rows.HasMore,
                NextCursor = rows.NextCursor,
                LoadedCount = rows.LoadedCount,
                TotalEstimated = rows.TotalEstimated
            }
        };
    }

    // ══════════════════════════════════════════════
    // Reference API
    // ══════════════════════════════════════════════

    public AdminReferenceDto GetReference()
    {
        return new AdminReferenceDto
        {
            RowTypes = new List<AdminEnumValueDto>
            {
                new() { Value = RowDefinitionConstants.RowType.IndustryHeader, Label = "Tiêu đề ngành nghề" },
                new() { Value = RowDefinitionConstants.RowType.DataPlaceholder, Label = "Vùng dữ liệu" },
                new() { Value = RowDefinitionConstants.RowType.Subtotal, Label = "Cộng nhóm" },
                new() { Value = RowDefinitionConstants.RowType.TaxLine, Label = "Dòng thuế" },
                new() { Value = RowDefinitionConstants.RowType.GrandTotal, Label = "Tổng cộng" },
                new() { Value = RowDefinitionConstants.RowType.SectionHeader, Label = "Tiêu đề phần" },
                new() { Value = RowDefinitionConstants.RowType.SectionSubtotal, Label = "Cộng phần" },
                new() { Value = RowDefinitionConstants.RowType.BalanceRow, Label = "Dòng số dư" },
                new() { Value = RowDefinitionConstants.RowType.MonthlyTotal, Label = "Cộng tháng" },
                new() { Value = RowDefinitionConstants.RowType.QuarterlyTotal, Label = "Cộng quý" },
                new() { Value = RowDefinitionConstants.RowType.ProfitRow, Label = "Chênh lệch DT-CP" }
            },
            Positions = new List<AdminEnumValueDto>
            {
                new() { Value = RowDefinitionConstants.Position.PerGroup, Label = "Mỗi nhóm" },
                new() { Value = RowDefinitionConstants.Position.PerSection, Label = "Mỗi phần" },
                new() { Value = RowDefinitionConstants.Position.StartOfBook, Label = "Đầu sổ" },
                new() { Value = RowDefinitionConstants.Position.EndOfBook, Label = "Cuối sổ" }
            },
            SectionTypes = new List<AdminEnumValueDto>
            {
                new() { Value = RowDefinitionConstants.SectionType.IndustryGroup, Label = "Nhóm ngành nghề" },
                new() { Value = RowDefinitionConstants.SectionType.RevenueCost, Label = "Doanh thu / Chi phí" },
                new() { Value = RowDefinitionConstants.SectionType.CashBank, Label = "Tiền mặt / Ngân hàng" },
                new() { Value = RowDefinitionConstants.SectionType.PerProduct, Label = "Theo sản phẩm" }
            },
            FieldTypes = new List<AdminEnumValueDto>
            {
                new() { Value = "auto_increment", Label = "STT tự tăng" },
                new() { Value = "date", Label = "Ngày tháng" },
                new() { Value = "text", Label = "Văn bản" },
                new() { Value = "decimal", Label = "Số thập phân" },
                new() { Value = "computed", Label = "Tính toán" }
            },
            SourceTypes = new List<AdminEnumValueDto>
            {
                new() { Value = "query", Label = "Truy vấn từ DB" },
                new() { Value = "formula", Label = "Công thức" },
                new() { Value = "static", Label = "Giá trị cố định" },
                new() { Value = "auto", Label = "Tự động" }
            },
            TaxTypes = new List<AdminEnumValueDto>
            {
                new() { Value = RowDefinitionConstants.TaxType.Vat, Label = "Thuế GTGT" },
                new() { Value = RowDefinitionConstants.TaxType.Pit, Label = "Thuế TNCN" }
            }
        };
    }

    // ══════════════════════════════════════════════
    // MappableEntities CRUD
    // ══════════════════════════════════════════════

    public async Task<List<AdminMappableEntityDto>> GetMappableEntitiesAsync(bool? active)
    {
        var entities = await _uow.AccountingTemplates.GetAllMappableEntitiesAsync(active);
        return entities.Select(MapMappableEntity).ToList();
    }

    public async Task<AdminMappableEntityDetailDto> GetMappableEntityDetailAsync(int entityId)
    {
        var entity = await _uow.AccountingTemplates.GetMappableEntityWithFieldsAsync(entityId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        return new AdminMappableEntityDetailDto
        {
            EntityId = entity.EntityId,
            EntityCode = entity.EntityCode,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Category = entity.Category,
            IsActive = entity.IsActive,
            FieldCount = entity.Fields.Count,
            Fields = entity.Fields.Select(MapMappableField).ToList()
        };
    }

    public async Task<AdminMappableEntityDto> CreateMappableEntityAsync(CreateMappableEntityRequest request, Guid actorUserId)
    {
        var existing = await _uow.AccountingTemplates.GetMappableEntityByCodeAsync(request.EntityCode.Trim());
        if (existing != null)
            throw new BadRequestException(MessageKeys.BadRequest, $"EntityCode '{request.EntityCode}' already exists");

        var entity = new MappableEntity
        {
            EntityCode = request.EntityCode.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Description = request.Description,
            Category = request.Category.Trim(),
            IsActive = true,
            CreatedByUserId = actorUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.AccountingTemplates.AddMappableEntityAsync(entity);
        await _uow.SaveChangesAsync();

        return MapMappableEntity(entity);
    }

    public async Task<AdminMappableEntityDto> UpdateMappableEntityAsync(int entityId, UpdateMappableEntityRequest request)
    {
        var entity = await _uow.AccountingTemplates.GetMappableEntityWithFieldsAsync(entityId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (request.DisplayName != null)
            entity.DisplayName = request.DisplayName.Trim();
        if (request.Description != null)
            entity.Description = request.Description;
        if (request.IsActive.HasValue)
            entity.IsActive = request.IsActive.Value;

        entity.UpdatedAt = DateTime.UtcNow;

        _uow.AccountingTemplates.UpdateMappableEntity(entity);
        await _uow.SaveChangesAsync();

        return MapMappableEntity(entity);
    }

    // ══════════════════════════════════════════════
    // MappableFields CRUD
    // ══════════════════════════════════════════════

    public async Task<AdminMappableFieldDto> CreateMappableFieldAsync(int entityId, CreateMappableFieldRequest request)
    {
        var entity = await _uow.AccountingTemplates.GetMappableEntityWithFieldsAsync(entityId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (entity.Fields.Any(f => f.FieldCode == request.FieldCode.Trim()))
            throw new BadRequestException(MessageKeys.BadRequest, $"FieldCode '{request.FieldCode}' already exists on this entity");

        var field = new MappableField
        {
            EntityId = entityId,
            FieldCode = request.FieldCode.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Description = request.Description,
            DataType = request.DataType.Trim(),
            AllowedAggregations = request.AllowedAggregations,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.AccountingTemplates.AddMappableFieldAsync(field);
        await _uow.SaveChangesAsync();

        return MapMappableField(field);
    }

    public async Task<AdminMappableFieldDto> UpdateMappableFieldAsync(int fieldId, UpdateMappableFieldRequest request)
    {
        var field = await _uow.AccountingTemplates.GetMappableFieldByIdAsync(fieldId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (request.DisplayName != null) field.DisplayName = request.DisplayName.Trim();
        if (request.Description != null) field.Description = request.Description;
        if (request.DataType != null) field.DataType = request.DataType.Trim();
        if (request.AllowedAggregations != null) field.AllowedAggregations = request.AllowedAggregations;
        if (request.IsActive.HasValue) field.IsActive = request.IsActive.Value;

        field.UpdatedAt = DateTime.UtcNow;

        _uow.AccountingTemplates.UpdateMappableField(field);
        await _uow.SaveChangesAsync();

        return MapMappableField(field);
    }

    // ══════════════════════════════════════════════
    // RowDefinitions CRUD
    // ══════════════════════════════════════════════

    public async Task<List<AdminRowDefinitionDto>> GetRowDefinitionsAsync(int templateVersionId)
    {
        var rows = await _uow.AccountingTemplates.GetRowDefinitionsAsync(templateVersionId);
        return rows.Select(MapRowDefinition).ToList();
    }

    public async Task<AdminRowDefinitionDto> CreateRowDefinitionAsync(int templateVersionId, CreateRowDefinitionRequest request)
    {
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (version.IsActive && version.AccountingBooks.Any())
            throw new BadRequestException(MessageKeys.BadRequest, "Cannot add rows to active version with books");

        if (!RowDefinitionConstants.RowType.All.Contains(request.RowType))
            throw new BadRequestException(MessageKeys.BadRequest, $"Invalid RowType: {request.RowType}");

        if (!RowDefinitionConstants.Position.All.Contains(request.Position))
            throw new BadRequestException(MessageKeys.BadRequest, $"Invalid Position: {request.Position}");

        var rowDef = new TemplateRowDefinition
        {
            TemplateVersionId = templateVersionId,
            RowType = request.RowType,
            RowLabel = request.RowLabel,
            Position = request.Position,
            SortOrder = request.SortOrder,
            GroupByField = request.GroupByField,
            SectionType = request.SectionType,
            SectionFilterValue = request.SectionFilterValue,
            VisibleFieldCodes = request.VisibleFieldCodes,
            FormulaId = request.FormulaId,
            TaxType = request.TaxType,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.AccountingTemplates.AddRowDefinitionAsync(rowDef);
        await _uow.SaveChangesAsync();

        return MapRowDefinition(rowDef);
    }

    public async Task<AdminRowDefinitionDto> UpdateRowDefinitionAsync(int rowDefId, UpdateRowDefinitionRequest request)
    {
        var rowDef = await _uow.AccountingTemplates.GetRowDefinitionByIdAsync(rowDefId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (request.RowType != null)
        {
            if (!RowDefinitionConstants.RowType.All.Contains(request.RowType))
                throw new BadRequestException(MessageKeys.BadRequest, $"Invalid RowType: {request.RowType}");
            rowDef.RowType = request.RowType;
        }
        if (request.RowLabel != null) rowDef.RowLabel = request.RowLabel;
        if (request.Position != null)
        {
            if (!RowDefinitionConstants.Position.All.Contains(request.Position))
                throw new BadRequestException(MessageKeys.BadRequest, $"Invalid Position: {request.Position}");
            rowDef.Position = request.Position;
        }
        if (request.SortOrder.HasValue) rowDef.SortOrder = request.SortOrder.Value;
        if (request.GroupByField != null) rowDef.GroupByField = request.GroupByField;
        if (request.SectionType != null) rowDef.SectionType = request.SectionType;
        if (request.SectionFilterValue != null) rowDef.SectionFilterValue = request.SectionFilterValue;
        if (request.VisibleFieldCodes != null) rowDef.VisibleFieldCodes = request.VisibleFieldCodes;
        if (request.FormulaId.HasValue) rowDef.FormulaId = request.FormulaId;
        if (request.TaxType != null) rowDef.TaxType = request.TaxType;

        _uow.AccountingTemplates.UpdateRowDefinition(rowDef);
        await _uow.SaveChangesAsync();

        return MapRowDefinition(rowDef);
    }

    public async Task DeleteRowDefinitionAsync(int rowDefId)
    {
        var rowDef = await _uow.AccountingTemplates.GetRowDefinitionByIdAsync(rowDefId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var version = rowDef.TemplateVersion;
        if (version.IsActive && version.AccountingBooks.Any())
            throw new BadRequestException(MessageKeys.BadRequest, "Cannot delete rows from active version with books");

        _uow.AccountingTemplates.RemoveRowDefinition(rowDef);
        await _uow.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════
    // FieldMappings create/delete
    // ══════════════════════════════════════════════

    public async Task<AdminTemplateFieldMappingDto> CreateFieldMappingAsync(int templateVersionId, CreateFieldMappingRequest request)
    {
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (version.IsActive && version.AccountingBooks.Any())
            throw new BadRequestException(MessageKeys.BadRequest, "Cannot add mappings to active version with books");

        if (version.FieldMappings.Any(m => m.FieldCode == request.FieldCode.Trim()))
            throw new BadRequestException(MessageKeys.BadRequest, $"FieldCode '{request.FieldCode}' already exists");

        var mapping = new TemplateFieldMapping
        {
            TemplateVersionId = templateVersionId,
            FieldCode = request.FieldCode.Trim(),
            FieldLabel = request.FieldLabel.Trim(),
            FieldType = request.FieldType,
            SourceType = request.SourceType,
            SourceEntityId = request.SourceEntityId,
            SourceFieldId = request.SourceFieldId,
            FilterJson = request.FilterJson,
            AggregationType = request.AggregationType,
            FormulaId = request.FormulaId,
            FormulaExpression = request.FormulaExpression,
            SortOrder = request.SortOrder,
            IsRequired = request.IsRequired
        };

        await _uow.AccountingTemplates.AddMappingAsync(mapping);
        await _uow.SaveChangesAsync();

        return new AdminTemplateFieldMappingDto
        {
            MappingId = mapping.MappingId,
            FieldCode = mapping.FieldCode,
            FieldLabel = mapping.FieldLabel,
            FieldType = mapping.FieldType,
            SourceType = mapping.SourceType,
            SourceEntityId = mapping.SourceEntityId,
            SourceFieldId = mapping.SourceFieldId,
            FilterJson = mapping.FilterJson,
            AggregationType = mapping.AggregationType,
            FormulaId = mapping.FormulaId,
            FormulaExpression = mapping.FormulaExpression,
            SortOrder = mapping.SortOrder
        };
    }

    public async Task DeleteFieldMappingAsync(int mappingId)
    {
        var mapping = await _uow.AccountingTemplates.GetMappingByIdAsync(mappingId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var version = mapping.TemplateVersion;
        if (version.IsActive && version.AccountingBooks.Any())
            throw new BadRequestException(MessageKeys.BadRequest, "Cannot delete mapping from active version with books");

        _uow.AccountingTemplates.RemoveMapping(mapping);
        await _uow.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════
    // Full structure
    // ══════════════════════════════════════════════

    public async Task<AdminFullStructureDto> GetFullStructureAsync(int templateVersionId)
    {
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAndBooksAsync(templateVersionId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var rows = await _uow.AccountingTemplates.GetRowDefinitionsAsync(templateVersionId);

        return new AdminFullStructureDto
        {
            TemplateVersionId = version.TemplateVersionId,
            TemplateCode = version.Template?.TemplateCode ?? string.Empty,
            TemplateName = version.Template?.Name ?? string.Empty,
            VersionLabel = version.VersionLabel,
            IsActive = version.IsActive,
            FieldMappings = version.FieldMappings
                .OrderBy(m => m.SortOrder)
                .Select(m => new AdminTemplateFieldMappingDto
                {
                    MappingId = m.MappingId,
                    FieldCode = m.FieldCode,
                    FieldLabel = m.FieldLabel,
                    FieldType = m.FieldType,
                    SourceType = m.SourceType,
                    SourceEntityId = m.SourceEntityId,
                    SourceFieldId = m.SourceFieldId,
                    FilterJson = m.FilterJson,
                    AggregationType = m.AggregationType,
                    FormulaId = m.FormulaId,
                    FormulaExpression = m.FormulaExpression,
                    SortOrder = m.SortOrder
                }).ToList(),
            RowDefinitions = rows.Select(MapRowDefinition).ToList(),
            RenderPreview = BuildRenderPreview(rows)
        };
    }

    // ══════════════════════════════════════════════
    // Private mappers
    // ══════════════════════════════════════════════

    private static AdminMappableEntityDto MapMappableEntity(MappableEntity entity)
    {
        return new AdminMappableEntityDto
        {
            EntityId = entity.EntityId,
            EntityCode = entity.EntityCode,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Category = entity.Category,
            IsActive = entity.IsActive,
            FieldCount = entity.Fields.Count
        };
    }

    private static AdminMappableFieldDto MapMappableField(MappableField field)
    {
        return new AdminMappableFieldDto
        {
            FieldId = field.FieldId,
            EntityId = field.EntityId,
            FieldCode = field.FieldCode,
            DisplayName = field.DisplayName,
            Description = field.Description,
            DataType = field.DataType,
            AllowedAggregations = field.AllowedAggregations,
            IsActive = field.IsActive
        };
    }

    private static AdminRowDefinitionDto MapRowDefinition(TemplateRowDefinition r)
    {
        return new AdminRowDefinitionDto
        {
            RowDefId = r.RowDefId,
            TemplateVersionId = r.TemplateVersionId,
            RowType = r.RowType,
            RowLabel = r.RowLabel,
            Position = r.Position,
            SortOrder = r.SortOrder,
            GroupByField = r.GroupByField,
            SectionType = r.SectionType,
            SectionFilterValue = r.SectionFilterValue,
            VisibleFieldCodes = r.VisibleFieldCodes,
            FormulaId = r.FormulaId,
            FormulaCode = r.Formula?.Code,
            TaxType = r.TaxType
        };
    }

    private static string BuildRenderPreview(List<TemplateRowDefinition> rows)
    {
        if (rows.Count == 0) return "(empty)";

        var parts = new List<string>();
        foreach (var r in rows.OrderBy(x => x.SortOrder))
        {
            var label = r.RowLabel ?? r.RowType;
            parts.Add(r.RowType switch
            {
                "section_header" => $"\n[{label}]",
                "data_placeholder" => "  → data rows...",
                "balance_row" => $"  {label} (formula)",
                "subtotal" => $"  Σ {label}",
                "tax_line" => $"  税 {label} ({r.TaxType})",
                "grand_total" => $"═══ {label}",
                _ => $"  {label}"
            });
        }
        return string.Join("\n", parts).Trim();
    }

    private static AdminTemplateVersionDto MapTemplateVersion(AccountingTemplateVersion version)
    {
        return new AdminTemplateVersionDto
        {
            TemplateVersionId = version.TemplateVersionId,
            TemplateId = version.TemplateId,
            VersionLabel = version.VersionLabel,
            IsActive = version.IsActive,
            EffectiveFrom = version.EffectiveFrom,
            ChangeNotes = version.ChangeNotes,
            MappingCount = version.FieldMappings.Count,
            BookCount = version.AccountingBooks.Count
        };
    }

    private static string BuildDraftVersionLabel(string sourceLabel)
    {
        var suffix = "-d-" + DateTime.UtcNow.ToString("yyMMddHHmmss"); // 15 chars
        var maxPrefixLength = TemplateVersionLabelMaxLength - suffix.Length;
        if (maxPrefixLength < 1)
            return suffix[..TemplateVersionLabelMaxLength];

        var prefix = (sourceLabel ?? string.Empty).Trim();
        if (prefix.Length > maxPrefixLength)
            prefix = prefix[..maxPrefixLength];

        if (string.IsNullOrWhiteSpace(prefix))
            prefix = "v";

        return prefix + suffix;
    }

    private static AdminTaxRulesetDto MapTaxRuleset(TaxRuleset ruleset)
    {
        return new AdminTaxRulesetDto
        {
            RulesetId = ruleset.RulesetId,
            Code = ruleset.Code,
            Name = ruleset.Name,
            Version = ruleset.Version,
            IsActive = ruleset.IsActive,
            EffectiveFrom = ruleset.EffectiveFrom,
            EffectiveTo = ruleset.EffectiveTo,
            GroupRuleCount = ruleset.GroupRules.Count,
            IndustryRateCount = ruleset.IndustryTaxRates.Count,
            BookCount = ruleset.AccountingBooks.Count
        };
    }

    private static AdminFormulaDto MapFormula(FormulaDefinition formula)
    {
        return new AdminFormulaDto
        {
            FormulaId = formula.FormulaId,
            Code = formula.Code,
            Name = formula.Name,
            IsActive = formula.IsActive,
            FormulaType = formula.FormulaType,
            ExpressionJson = formula.ExpressionJson,
            Description = formula.Description
        };
    }
}
