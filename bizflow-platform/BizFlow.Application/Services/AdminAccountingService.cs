using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
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
