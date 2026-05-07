using System.Text.Json;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Helpers;
using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;

namespace BizFlow.Application.Services;

public class AdminAccountingService : IAdminAccountingService
{
    private const int TemplateVersionLabelMaxLength = 50;

    private readonly IUnitOfWork _uow;
    private readonly IBookRenderingService _renderingService;
    private readonly IFormulaEngine _formulaEngine;
    private readonly IGeneralLedgerService _generalLedgerService;

    public AdminAccountingService(
        IUnitOfWork uow,
        IBookRenderingService renderingService,
        IFormulaEngine formulaEngine,
        IGeneralLedgerService generalLedgerService)
    {
        _uow = uow;
        _renderingService = renderingService;
        _formulaEngine = formulaEngine;
        _generalLedgerService = generalLedgerService;
    }

    public async Task<AdminAccountingOverviewDto> GetOverviewAsync()
    {
        var templates = await _uow.AccountingTemplates.GetAllWithVersionsAsync();
        var rulesets = await _uow.TaxRulesets.GetAllWithRulesAsync();
        var formulas = await _uow.FormulaDefinitions.GetAllAsync();
        var businessTypes = await _uow.BusinessTypes.GetAllAsync();

        return new AdminAccountingOverviewDto
        {
            Templates = templates.Select(MapTemplate).ToList(),
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
                    SourceEntityCode = m.SourceEntity?.EntityCode,
                    SourceEntityDisplayName = m.SourceEntity?.DisplayName,
                    SourceFieldId = m.SourceFieldId,
                    SourceFieldCode = m.SourceField?.FieldCode,
                    SourceFieldDisplayName = m.SourceField?.DisplayName,
                    FilterJson = m.FilterJson,
                    AggregationType = m.AggregationType,
                    FormulaId = m.FormulaId,
                    FormulaCode = m.Formula?.Code,
                    FormulaName = m.Formula?.Name,
                    FormulaExpression = m.FormulaExpression,
                    DependsOn = m.DependsOn,
                    CalculationOrder = m.CalculationOrder,
                    ExportColumn = m.ExportColumn,
                    SortOrder = m.SortOrder,
                    IsRequired = m.IsRequired
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

    public async Task<AdminTemplateDto> CreateTemplateAsync(CreateTemplateRequest request, Guid actorUserId)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateCode))
            throw new BadRequestException(MessageKeys.AdminAccTemplateCodeRequired);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BadRequestException(MessageKeys.AdminAccNameRequired);
        if (request.ApplicableGroups == null || request.ApplicableGroups.Count == 0)
            throw new BadRequestException(MessageKeys.AdminAccApplicableGroupsRequired);

        var validDataSources = new[] { "revenues", "revenue_cost", "gl_entries", "stock_movements" };
        if (!validDataSources.Contains(request.DataSourceType))
            throw new BadRequestException(
                MessageKeys.AdminAccInvalidDataSourceType,
                null,
                string.Join(", ", validDataSources));

        var code = request.TemplateCode.Trim().ToUpper();
        if (await _uow.AccountingTemplates.ExistsByCodeAsync(code))
            throw new BadRequestException(MessageKeys.AdminAccTemplateCodeExists, null, code);

        var versionLabel = string.IsNullOrWhiteSpace(request.InitialVersionLabel)
            ? "v1-draft"
            : request.InitialVersionLabel.Trim();

        var template = new AccountingTemplate
        {
            TemplateCode = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ApplicableGroups = JsonSerializer.Serialize(request.ApplicableGroups.Distinct().ToList()),
            ApplicableMethods = request.ApplicableMethods != null && request.ApplicableMethods.Count > 0
                ? JsonSerializer.Serialize(request.ApplicableMethods)
                : null,
            DataSourceType = request.DataSourceType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Versions = new List<AccountingTemplateVersion>
            {
                new AccountingTemplateVersion
                {
                    VersionLabel = versionLabel,
                    IsActive = false,
                    CreatedByUserId = actorUserId,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        await _uow.AccountingTemplates.AddTemplateAsync(template);
        await _uow.SaveChangesAsync();

        return MapTemplate(template);
    }

    public async Task<AdminTemplateVersionDto> CreateTemplateVersionAsync(int templateId, CreateTemplateVersionRequest request, Guid actorUserId)
    {
        if (string.IsNullOrWhiteSpace(request.VersionLabel))
            throw new BadRequestException(MessageKeys.AdminAccVersionLabelRequired);
        if (request.VersionLabel.Length > TemplateVersionLabelMaxLength)
            throw new BadRequestException(MessageKeys.AdminAccVersionLabelTooLong, null, TemplateVersionLabelMaxLength);

        var template = await _uow.AccountingTemplates.GetByIdWithVersionsAsync(templateId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var labelTrimmed = request.VersionLabel.Trim();
        if (template.Versions.Any(v => v.VersionLabel == labelTrimmed))
            throw new BadRequestException(MessageKeys.AdminAccVersionLabelExists, null, labelTrimmed);

        var version = new AccountingTemplateVersion
        {
            TemplateId = templateId,
            VersionLabel = labelTrimmed,
            IsActive = false,
            EffectiveFrom = request.EffectiveFrom,
            ChangeNotes = request.ChangeNotes?.Trim(),
            CreatedByUserId = actorUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.AccountingTemplates.AddVersionAsync(version);
        await _uow.SaveChangesAsync();

        return MapTemplateVersion(version);
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
                    MessageKeys.AdminAccVersionLabelTooLong,
                    null,
                    TemplateVersionLabelMaxLength);
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

        // Only deactivate versions that share the same effective period.
        // Versions with a different EffectiveFrom can remain active concurrently.
        foreach (var v in template.Versions)
        {
            if (v.TemplateVersionId == templateVersionId)
                v.IsActive = true;
            else if (v.IsActive && v.EffectiveFrom == version.EffectiveFrom)
                v.IsActive = false;
        }

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
            var activeSiblings = templates
                .Where(t => t.TemplateId == version.TemplateId)
                .SelectMany(t => t.Versions)
                .Where(v => v.TemplateVersionId != templateVersionId
                         && v.IsActive)
                .ToList();

            if (!activeSiblings.Any())
                throw new BadRequestException(MessageKeys.TemplateMustHaveAtLeastOneActiveVersion);
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
            throw new BadRequestException(MessageKeys.AdminAccDeleteActiveVersionNotAllowed);

        if (version.AccountingBooks.Any())
            throw new BadRequestException(MessageKeys.AdminAccDeleteOnlyDraftAllowed);

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
        {
            var oldFormulaId = mapping.FormulaId;
            mapping.FormulaId = request.FormulaId;
            // When formula ID changes, clear expression text so rendering uses formula ID instead
            mapping.FormulaExpression = null;

            // Cascade: update any RowDefinition in the same version that still references the old formula.
            // This keeps tax_line / balance_row rows in sync when admin remaps a formula-type field mapping.
            if (oldFormulaId.HasValue
                && oldFormulaId.Value != request.FormulaId.Value
                && string.Equals(mapping.SourceType, "formula", StringComparison.OrdinalIgnoreCase))
            {
                var rowDefs = await _uow.AccountingTemplates.GetRowDefinitionsAsync(mapping.TemplateVersionId);
                var affected = rowDefs.Where(r => r.FormulaId == oldFormulaId.Value).ToList();
                foreach (var rd in affected)
                {
                    rd.FormulaId = request.FormulaId.Value;
                    _uow.AccountingTemplates.UpdateRowDefinition(rd);
                }
            }
        }
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
            DependsOn = mapping.DependsOn,
            CalculationOrder = mapping.CalculationOrder,
            ExportColumn = mapping.ExportColumn,
            SortOrder = mapping.SortOrder,
            IsRequired = mapping.IsRequired
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

        var hasNonStatusChanges =
            request.Code != null
            || request.Name != null
            || request.Description != null
            || request.FormulaType != null
            || request.ExpressionJson != null;

        // Active formulas are locked for content edits, but status toggle (active -> inactive) is allowed.
        if (formula.IsActive && hasNonStatusChanges)
            throw new BadRequestException(MessageKeys.AdminAccCannotEditActiveFormula);

        if (request.Code != null)
        {
            var code = request.Code.Trim();
            if (string.IsNullOrEmpty(code))
                throw new BadRequestException(MessageKeys.AdminAccCodeRequired);

            var existingWithCode = (await _uow.FormulaDefinitions.GetAllAsync())
                .FirstOrDefault(f => f.FormulaId != formulaId
                    && string.Equals(f.Code, code, StringComparison.OrdinalIgnoreCase));
            if (existingWithCode != null)
                throw new BadRequestException(MessageKeys.AdminAccFormulaCodeExists, null, code);

            formula.Code = code;
        }

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

    public async Task<AdminFormulaDto> CreateFormulaAsync(CreateFormulaRequest request, Guid actorUserId)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException(MessageKeys.AdminAccCodeRequired);
        if (string.IsNullOrWhiteSpace(request.ExpressionJson))
            throw new BadRequestException(MessageKeys.AdminAccExpressionJsonRequired);

        // Validate JSON
        try { System.Text.Json.JsonDocument.Parse(request.ExpressionJson); }
        catch { throw new BadRequestException(MessageKeys.AdminAccExpressionJsonInvalid); }

        var existing = (await _uow.FormulaDefinitions.GetAllAsync())
            .FirstOrDefault(f => string.Equals(f.Code, request.Code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            throw new BadRequestException(MessageKeys.AdminAccFormulaCodeExists, null, request.Code);

        var formula = new FormulaDefinition
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            FormulaType = request.FormulaType ?? "computed",
            ExpressionJson = request.ExpressionJson,
            ResultDataType = request.ResultDataType ?? "decimal",
            RoundingMode = request.RoundingMode,
            RoundingPrecision = request.RoundingPrecision ?? 0,
            IsActive = false,
            CreatedByUserId = actorUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.FormulaDefinitions.AddAsync(formula);
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

    public async Task<bool> DeleteFormulaAsync(long formulaId)
    {
        var formula = await _uow.FormulaDefinitions.GetByIdAsync(formulaId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (formula.IsActive)
        {
            throw new BadRequestException(MessageKeys.FormulaCannotDeleteActive);
        }

        // Draft/inactive: remove FK blockers (template links + persisted book results), then hard-delete.
        await _uow.AccountingTemplates.ClearFormulaLinksAsync(formulaId);
        await _uow.FormulaResults.DeleteByFormulaIdAsync(formulaId);

        _uow.FormulaDefinitions.Delete(formula);
        await _uow.SaveChangesAsync();
        return true;
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
                throw new BadRequestException(MessageKeys.AdminAccCannotDeactivateOnlyActiveTaxRuleset);
        }

        ruleset.IsActive = false;
        _uow.TaxRulesets.Update(ruleset);
        await _uow.SaveChangesAsync();

        return MapTaxRuleset(ruleset);
    }

    public async Task<AdminPreviewResponse> PreviewAsync(AdminPreviewRequest request)
    {
        if (request.BusinessLocationId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccBusinessLocationIdMustBePositive);

        if (request.BusinessLocationId != 6)
            throw new BadRequestException(MessageKeys.AdminAccTestLocationOnly);

        if (request.PeriodId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccPeriodIdMustBePositive);

        if (request.TemplateVersionId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccTemplateVersionIdMustBePositive);

        if (request.RulesetId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccRulesetIdMustBePositive);

        if (request.BatchSize < 1)
            request.BatchSize = 1;
        if (request.BatchSize > 200)
            request.BatchSize = 200;

        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAsync(request.TemplateVersionId)
            ?? throw new NotFoundException(MessageKeys.AdminAccTemplateVersionNotFound, request.TemplateVersionId);

        var period = await _uow.AccountingPeriods.GetByLocationAndIdAsync(request.BusinessLocationId, request.PeriodId)
            ?? throw new NotFoundException(
                MessageKeys.PeriodNotFound,
                $"LocationId={request.BusinessLocationId}",
                $"PeriodId={request.PeriodId}");

        var ruleset = await _uow.TaxRulesets.GetByIdWithRulesAsync(request.RulesetId)
            ?? throw new NotFoundException(MessageKeys.AdminAccTaxRulesetNotFound, request.RulesetId);

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
                    .Where(r => r.Status != RevenueStatus.Cancelled
                        && r.BusinessTypeId.HasValue)
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
            DataSourceType = version.Template?.DataSourceType ?? "revenues",
            GroupNumber = request.GroupNumber,
            TaxMethod = request.TaxMethod,
            RulesetId = ruleset.RulesetId,
            BusinessTypeIds = businessTypeIds
        };

        var summary = await _renderingService.ComputeSummaryAsync(ctx);
        var rows = await _renderingService.RenderRowsAsync(ctx, null, request.BatchSize);
        var sectionsResult = await _renderingService.RenderSectionsAsync(ctx);

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
            },
            Columns = sectionsResult.Columns,
            Sections = sectionsResult.Sections,
            FooterRows = sectionsResult.FooterRows
        };
    }

    public async Task<GenerateConsultantSampleDataResponse> GenerateConsultantSampleDataAsync(
        GenerateConsultantSampleDataRequest request,
        Guid actorUserId)
    {
        if (request.BusinessLocationId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccBusinessLocationIdMustBePositive);

        var location = await _uow.BusinessLocations.GetByIdAsync(request.BusinessLocationId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var candidateBusinessTypeIds = await ResolveBusinessTypeCandidatesAsync(request.BusinessLocationId);
        var selectedBusinessTypeId = candidateBusinessTypeIds.FirstOrDefault();

        var baseDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var revenueSeeds = new List<(decimal Amount, string Description, string MoneyChannel, Guid? BusinessTypeId)>
        {
            (4200000m, "Doanh thu mẫu tư vấn theo giờ", MoneyChannelType.Bank, selectedBusinessTypeId),
            (1850000m, "Doanh thu mẫu bán gói dịch vụ nhanh", MoneyChannelType.Cash, selectedBusinessTypeId),
            (2760000m, "Doanh thu mẫu hỗ trợ vận hành", MoneyChannelType.Bank, selectedBusinessTypeId),
            (1390000m, "Doanh thu mẫu đào tạo nội bộ", MoneyChannelType.Cash, selectedBusinessTypeId),
            (3250000m, "Doanh thu mẫu tối ưu quy trình", MoneyChannelType.Bank, selectedBusinessTypeId)
        };

        var costSeeds = new List<(decimal Amount, string Description, string CostType, string PaymentMethod, Guid? BusinessTypeId)>
        {
            (980000m, "Chi phí mẫu thuê cộng tác viên", CostType.Salary, MoneyChannelType.Bank, selectedBusinessTypeId),
            (540000m, "Chi phí mẫu điện nước văn phòng", CostType.Utilities, MoneyChannelType.Cash, selectedBusinessTypeId),
            (760000m, "Chi phí mẫu quảng bá dịch vụ", CostType.Marketing, MoneyChannelType.Bank, selectedBusinessTypeId),
            (630000m, "Chi phí mẫu di chuyển gặp khách", CostType.Transport, MoneyChannelType.Cash, selectedBusinessTypeId),
            (450000m, "Chi phí mẫu văn phòng phẩm", CostType.Other, MoneyChannelType.Cash, selectedBusinessTypeId)
        };

        var createdRevenueIds = new List<long>();
        var createdCostIds = new List<long>();

        foreach (var (seed, index) in revenueSeeds.Select((value, idx) => (value, idx)))
        {
            var revenue = await EntityCodeGenerator.ExecuteWithDuplicateKeyRetryAsync(() =>
                _uow.ExecuteResilientAsync(async ct =>
                {
                    var entity = new Revenue
                    {
                        RevenueCode = EntityCodeGenerator.Generate("REV", DateTime.UtcNow, location.BusinessLocationId, 5),
                        BusinessLocationId = location.BusinessLocationId,
                        BusinessTypeId = seed.BusinessTypeId,
                        RevenueType = RevenueType.Manual,
                        Amount = seed.Amount,
                        Status = RevenueStatus.Posted,
                        RevenueDate = baseDate.AddDays(-index),
                        Description = seed.Description,
                        MoneyChannel = seed.MoneyChannel,
                        CreatedBy = actorUserId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.Revenues.AddAsync(entity);
                    await _uow.SaveChangesAsync(ct);
                    await _generalLedgerService.RecordRevenueLedgerLineFromRowAsync(entity);
                    await _uow.SaveChangesAsync(ct);
                    return entity;
                }));

            createdRevenueIds.Add(revenue.RevenueId);
        }

        foreach (var (seed, index) in costSeeds.Select((value, idx) => (value, idx)))
        {
            var cost = await EntityCodeGenerator.ExecuteWithDuplicateKeyRetryAsync(() =>
                _uow.ExecuteResilientAsync(async ct =>
                {
                    var entity = new Cost
                    {
                        CostCode = EntityCodeGenerator.Generate("COST", DateTime.UtcNow, location.BusinessLocationId, 5),
                        BusinessLocationId = location.BusinessLocationId,
                        BusinessTypeId = seed.BusinessTypeId,
                        CostType = seed.CostType,
                        Description = seed.Description,
                        Amount = seed.Amount,
                        Status = CostStatus.Posted,
                        CostDate = baseDate.AddDays(-index),
                        PaymentMethod = seed.PaymentMethod,
                        CreatedBy = actorUserId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.Costs.AddAsync(entity);
                    await _uow.SaveChangesAsync(ct);
                    await _generalLedgerService.RecordCostLedgerLineFromRowAsync(entity);
                    await _uow.SaveChangesAsync(ct);
                    return entity;
                }));

            createdCostIds.Add(cost.CostId);
        }

        return new GenerateConsultantSampleDataResponse
        {
            BusinessLocationId = request.BusinessLocationId,
            RevenueCreatedCount = createdRevenueIds.Count,
            CostCreatedCount = createdCostIds.Count,
            RevenueIds = createdRevenueIds,
            CostIds = createdCostIds
        };
    }

    // ══════════════════════════════════════════════
    // Compare API
    // ══════════════════════════════════════════════

    public async Task<AdminCompareResponse> CompareAsync(AdminCompareRequest request)
    {
        if (request.BusinessLocationId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccBusinessLocationIdMustBePositive);
        if (request.PeriodId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccPeriodIdMustBePositive);
        if (request.DraftVersionId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccDraftVersionIdMustBePositive);
        if (request.RulesetId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccRulesetIdMustBePositive);

        // Resolve active version if not provided
        var activeVersionId = request.ActiveVersionId;
        if (!activeVersionId.HasValue || activeVersionId.Value <= 0)
        {
            var draftVersion = await _uow.AccountingTemplates.GetVersionWithMappingsAsync(request.DraftVersionId)
                ?? throw new NotFoundException(MessageKeys.AdminAccTemplateVersionNotFound, request.DraftVersionId);

            var templates = await _uow.AccountingTemplates.GetAllWithVersionsAsync();
            var template = templates.FirstOrDefault(t => t.TemplateId == draftVersion.TemplateId);
            activeVersionId = template?.Versions.FirstOrDefault(v => v.IsActive)?.TemplateVersionId;

            if (!activeVersionId.HasValue)
                throw new BadRequestException(MessageKeys.AdminAccNoActiveTemplateVersionForCompare);
        }

        // Run both previews
        var activePreview = await RunPreviewForCompare(activeVersionId.Value, request);
        var draftPreview = await RunPreviewForCompare(request.DraftVersionId, request);

        // Compute diff
        var diff = ComputeCompareDiff(activePreview, draftPreview);

        return new AdminCompareResponse
        {
            Active = activePreview,
            Draft = draftPreview,
            Diff = diff
        };
    }

    private async Task<AdminPreviewResponse> RunPreviewForCompare(int templateVersionId, AdminCompareRequest request)
    {
        var previewRequest = new AdminPreviewRequest
        {
            BusinessLocationId = request.BusinessLocationId,
            PeriodId = request.PeriodId,
            TemplateVersionId = templateVersionId,
            GroupNumber = request.GroupNumber,
            TaxMethod = request.TaxMethod,
            RulesetId = request.RulesetId,
            BusinessTypeIds = request.BusinessTypeIds,
            BatchSize = request.BatchSize
        };
        return await PreviewAsync(previewRequest);
    }

    private static AdminCompareDiff ComputeCompareDiff(AdminPreviewResponse active, AdminPreviewResponse draft)
    {
        var changes = new List<FormulaValueChange>();
        var changedFormulas = new List<string>();

        var activeFormulas = active.Summary.FormulaValues ?? new Dictionary<string, decimal>();
        var draftFormulas = draft.Summary.FormulaValues ?? new Dictionary<string, decimal>();
        var allKeys = activeFormulas.Keys.Union(draftFormulas.Keys).Distinct();

        foreach (var key in allKeys)
        {
            var before = activeFormulas.GetValueOrDefault(key, 0m);
            var after = draftFormulas.GetValueOrDefault(key, 0m);
            if (before != after)
            {
                changedFormulas.Add(key);
                changes.Add(new FormulaValueChange { Code = key, Before = before, After = after });
            }
        }

        return new AdminCompareDiff
        {
            ChangedFormulas = changedFormulas,
            ValueChanges = changes
        };
    }

    // ══════════════════════════════════════════════
    // Trace API
    // ══════════════════════════════════════════════

    public async Task<AdminTraceResponse> TraceFormulaAsync(AdminTraceRequest request)
    {
        if (request.FormulaId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccFormulaIdMustBePositive);
        if (request.BusinessLocationId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccBusinessLocationIdMustBePositive);

        if (request.BusinessLocationId != 6)
            throw new BadRequestException(MessageKeys.AdminAccTestLocationOnly);
        if (request.PeriodId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccPeriodIdMustBePositive);
        if (request.RulesetId <= 0)
            throw new BadRequestException(MessageKeys.AdminAccRulesetIdMustBePositive);

        var formula = await _uow.FormulaDefinitions.GetByIdAsync(request.FormulaId)
            ?? throw new NotFoundException(MessageKeys.AdminAccFormulaNotFound, request.FormulaId);

        var period = await _uow.AccountingPeriods.GetByLocationAndIdAsync(request.BusinessLocationId, request.PeriodId)
            ?? throw new NotFoundException(MessageKeys.PeriodNotFound);

        var ctx = new FormulaEvaluationContext
        {
            BookId = 0,
            BusinessLocationId = request.BusinessLocationId,
            PeriodId = request.PeriodId,
            PeriodStart = period.StartDate,
            PeriodEnd = period.EndDate,
            RulesetId = request.RulesetId,
            BusinessTypeIds = request.BusinessTypeIds.Distinct().ToList()
        };

        var traceResult = await _formulaEngine.TraceFormulaAsync(ctx, formula);

        return new AdminTraceResponse
        {
            FormulaCode = formula.Code,
            FormulaName = formula.Name,
            FinalValue = traceResult.FinalValue,
            Trace = traceResult.Steps.Select(MapTraceNode).ToList()
        };
    }

    private static FormulaTraceStep MapTraceNode(FormulaTraceNode node)
    {
        return new FormulaTraceStep
        {
            Step = node.Step,
            NodeType = node.NodeType,
            Description = node.Description,
            ResolvedValue = node.ResolvedValue,
            Source = node.Source,
            Debug = node.Debug,
            Children = node.Children?.Select(MapTraceNode).ToList()
        };
    }

    private async Task<List<Guid>> ResolveBusinessTypeCandidatesAsync(int businessLocationId)
    {
        var fromProducts = await _uow.Products.QuickSearchByLocationAsync(businessLocationId, null);
        var ids = fromProducts
            .Where(p => p.DeletedAt == null)
            .Select(p => p.BusinessTypeId)
            .Distinct()
            .ToList();

        if (ids.Count > 0)
            return ids;

        var businessTypes = await _uow.BusinessTypes.GetAllAsync();
        return businessTypes
            .Where(bt => string.Equals(bt.Status, BusinessTypeConstants.Active, StringComparison.OrdinalIgnoreCase))
            .Select(bt => bt.BusinessTypeId)
            .ToList();
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
                new() { Value = RowDefinitionConstants.RowType.IndustryHeader, Label = "Tiêu đề ngành nghề", Description = "Dòng tiêu đề nhóm ngành nghề kinh doanh, hiển thị tên ngành" },
                new() { Value = RowDefinitionConstants.RowType.DataPlaceholder, Label = "Vùng dữ liệu", Description = "Vị trí chèn data rows từ query (revenues, costs, GL entries)" },
                new() { Value = RowDefinitionConstants.RowType.Subtotal, Label = "Cộng nhóm", Description = "Tổng cộng theo nhóm ngành, thường linked với formula SUM" },
                new() { Value = RowDefinitionConstants.RowType.TaxLine, Label = "Dòng thuế", Description = "Dòng hiển thị thuế (VAT/PIT), xuất hiện 1 lần cuối nhóm" },
                new() { Value = RowDefinitionConstants.RowType.GrandTotal, Label = "Tổng cộng", Description = "Dòng tổng cộng cuối sổ, tổng hợp tất cả nhóm" },
                new() { Value = RowDefinitionConstants.RowType.SectionHeader, Label = "Tiêu đề phần", Description = "Tiêu đề cho phần (revenue/cost/cash/bank)" },
                new() { Value = RowDefinitionConstants.RowType.SectionSubtotal, Label = "Cộng phần", Description = "Tổng cộng theo từng phần (section)" },
                new() { Value = RowDefinitionConstants.RowType.BalanceRow, Label = "Dòng số dư", Description = "Dòng hiển thị số dư (opening/closing balance), dùng formula lookup" },
                new() { Value = RowDefinitionConstants.RowType.MonthlyTotal, Label = "Cộng tháng", Description = "Tổng phát sinh trong tháng" },
                new() { Value = RowDefinitionConstants.RowType.QuarterlyTotal, Label = "Cộng quý", Description = "Tổng lũy kế trong quý" },
                new() { Value = RowDefinitionConstants.RowType.ProfitRow, Label = "Chênh lệch DT-CP", Description = "Dòng chênh lệch doanh thu - chi phí" }
            },
            Positions = new List<AdminEnumValueDto>
            {
                new() { Value = RowDefinitionConstants.Position.PerGroup, Label = "Mỗi nhóm", Description = "Lặp lại cho mỗi nhóm ngành nghề" },
                new() { Value = RowDefinitionConstants.Position.PerSection, Label = "Mỗi phần", Description = "Lặp lại cho mỗi section (revenue/cost)" },
                new() { Value = RowDefinitionConstants.Position.StartOfBook, Label = "Đầu sổ", Description = "Xuất hiện 1 lần ở đầu sổ" },
                new() { Value = RowDefinitionConstants.Position.EndOfBook, Label = "Cuối sổ", Description = "Xuất hiện 1 lần ở cuối sổ" }
            },
            SectionTypes = new List<AdminEnumValueDto>
            {
                new() { Value = RowDefinitionConstants.SectionType.IndustryGroup, Label = "Nhóm ngành nghề", Description = "Phân nhóm theo ngành nghề kinh doanh (BusinessType)" },
                new() { Value = RowDefinitionConstants.SectionType.RevenueCost, Label = "Doanh thu / Chi phí", Description = "Phân theo doanh thu và chi phí" },
                new() { Value = RowDefinitionConstants.SectionType.CashBank, Label = "Tiền mặt / Ngân hàng", Description = "Phân theo kênh tiền (cash/bank)" },
                new() { Value = RowDefinitionConstants.SectionType.PerProduct, Label = "Theo sản phẩm", Description = "Phân nhóm theo từng sản phẩm/dịch vụ" }
            },
            FieldTypes = new List<AdminEnumValueDto>
            {
                new() { Value = "auto_increment", Label = "STT tự tăng", Description = "Số thứ tự tự động tăng dần" },
                new() { Value = "date", Label = "Ngày tháng", Description = "Giá trị ngày tháng (yyyy-MM-dd)" },
                new() { Value = "text", Label = "Văn bản", Description = "Chuỗi ký tự" },
                new() { Value = "decimal", Label = "Số thập phân", Description = "Số thập phân (tiền, số lượng)" },
                new() { Value = "computed", Label = "Tính toán", Description = "Giá trị được tính từ formula" }
            },
            SourceTypes = new List<AdminEnumValueDto>
            {
                new() { Value = "query", Label = "Truy vấn từ DB", Description = "Lấy dữ liệu trực tiếp từ bảng (revenues, costs, gl_entries)" },
                new() { Value = "formula", Label = "Công thức", Description = "Tính toán bằng formula engine (ExpressionJson)" },
                new() { Value = "static", Label = "Giá trị cố định", Description = "Giá trị không đổi (ví dụ: label cột)" },
                new() { Value = "auto", Label = "Tự động", Description = "Giá trị tự động sinh (STT, ngày hiện tại)" }
            },
            TaxTypes = new List<AdminEnumValueDto>
            {
                new() { Value = RowDefinitionConstants.TaxType.Vat, Label = "Thuế GTGT", Description = "Thuế giá trị gia tăng" },
                new() { Value = RowDefinitionConstants.TaxType.Pit, Label = "Thuế TNCN", Description = "Thuế thu nhập cá nhân" }
            },
            FormulaNodeTypes = new List<AdminEnumValueDto>
            {
                new() { Value = "literal", Label = "Giá trị cố định", Description = "Hằng số (số cụ thể)", Example = "{\"literal\": 500000000}" },
                new() { Value = "ref", Label = "Tham chiếu formula", Description = "Lấy kết quả từ formula khác theo Code", Example = "{\"ref\": \"S2A_SUBTOTAL\"}" },
                new() { Value = "aggregate", Label = "Tổng hợp dữ liệu", Description = "SUM/AVG/COUNT từ bảng dữ liệu (revenues, costs, gl_entries)", Example = "{\"aggregate\":\"SUM\",\"source\":\"revenues\",\"field\":\"Amount\"}" },
                new() { Value = "lookup", Label = "Tra cứu ngoài", Description = "Tra cứu giá trị từ bảng khác (IndustryTaxRates, AccountingPeriods)", Example = "{\"lookup\":{\"entity\":\"IndustryTaxRates\",\"field\":\"TaxRate\",\"filter\":{\"TaxType\":\"VAT\"}}}" },
                new() { Value = "op", Label = "Phép toán", Description = "Phép tính 2 vế: ADD/SUBTRACT/MULTIPLY/DIVIDE", Example = "{\"op\":\"MULTIPLY\",\"left\":{\"ref\":\"S2A_SUBTOTAL\"},\"right\":{\"literal\":0.05}}" },
                new() { Value = "fn", Label = "Hàm", Description = "Hàm toán học: MAX/MIN/ABS", Example = "{\"fn\":\"MAX\",\"args\":[{\"literal\":0},{\"ref\":\"S2A_PROFIT\"}]}" },
                new() { Value = "context", Label = "Giá trị runtime", Description = "Giá trị từ context chạy (period_start, period_end)", Example = "{\"context\":\"period_start\"}" }
            },
            AggregateTypes = new List<AdminEnumValueDto>
            {
                new() { Value = "SUM", Label = "Tổng cộng", Description = "Tổng tất cả giá trị matching" },
                new() { Value = "AVG", Label = "Trung bình", Description = "Giá trị trung bình" },
                new() { Value = "COUNT", Label = "Đếm", Description = "Đếm số bản ghi matching" }
            },
            OpTypes = new List<AdminEnumValueDto>
            {
                new() { Value = "ADD", Label = "Cộng (+)", Description = "left + right" },
                new() { Value = "SUBTRACT", Label = "Trừ (−)", Description = "left - right" },
                new() { Value = "MULTIPLY", Label = "Nhân (×)", Description = "left × right" },
                new() { Value = "DIVIDE", Label = "Chia (÷)", Description = "left ÷ right (trả 0 nếu right = 0)" }
            },
            FnTypes = new List<AdminEnumValueDto>
            {
                new() { Value = "MAX", Label = "Giá trị lớn nhất", Description = "Lấy giá trị lớn nhất từ các args" },
                new() { Value = "MIN", Label = "Giá trị nhỏ nhất", Description = "Lấy giá trị nhỏ nhất từ các args" },
                new() { Value = "ABS", Label = "Giá trị tuyệt đối", Description = "Lấy giá trị tuyệt đối của arg đầu tiên" }
            }
        };
    }

    // ══════════════════════════════════════════════
    // Formula Node Schema
    // ══════════════════════════════════════════════

    public List<FormulaNodeSchemaDto> GetFormulaNodeSchemas()
    {
        return new List<FormulaNodeSchemaDto>
        {
            new()
            {
                NodeType = "literal", Label = "Giá trị cố định", Description = "Hằng số (số cụ thể)",
                Example = "{\"literal\": 500000000}",
                Fields = new() { new() { FieldName = "literal", FieldType = "number", Required = true, Description = "Giá trị số" } }
            },
            new()
            {
                NodeType = "ref", Label = "Tham chiếu formula", Description = "Lấy kết quả từ formula khác theo Code",
                Example = "{\"ref\": \"S2A_SUBTOTAL\"}",
                Fields = new() { new() { FieldName = "ref", FieldType = "string", Required = true, Description = "Code của formula được tham chiếu" } }
            },
            new()
            {
                NodeType = "aggregate", Label = "Tổng hợp dữ liệu", Description = "SUM/AVG/COUNT từ bảng dữ liệu",
                Example = "{\"aggregate\":\"SUM\",\"source\":\"revenues\",\"field\":\"Amount\",\"filter\":{\"RevenueType\":\"sale\"}}",
                Fields = new()
                {
                    new() { FieldName = "aggregate", FieldType = "enum", Required = true, Description = "Loại tổng hợp", AllowedValues = new() {"SUM","AVG","COUNT"} },
                    new() { FieldName = "source", FieldType = "string", Required = true, Description = "Bảng nguồn (revenues, costs, gl_entries)" },
                    new() { FieldName = "field", FieldType = "string", Required = true, Description = "Cột cần tổng hợp (Amount, DebitAmount, CreditAmount)" },
                    new() { FieldName = "filter", FieldType = "object", Required = false, Description = "Điều kiện lọc (key-value pairs)" }
                }
            },
            new()
            {
                NodeType = "lookup", Label = "Tra cứu ngoài", Description = "Tra cứu giá trị từ bảng khác",
                Example = "{\"lookup\":{\"entity\":\"IndustryTaxRates\",\"field\":\"TaxRate\",\"filter\":{\"TaxType\":\"VAT\"}}}",
                Fields = new()
                {
                    new() { FieldName = "lookup.entity", FieldType = "enum", Required = true, Description = "Bảng tra cứu", AllowedValues = new() {"IndustryTaxRates","AccountingPeriods"} },
                    new() { FieldName = "lookup.field", FieldType = "string", Required = true, Description = "Cột cần lấy giá trị" },
                    new() { FieldName = "lookup.filter", FieldType = "object", Required = false, Description = "Điều kiện lọc (ví dụ: TaxType=VAT)" }
                }
            },
            new()
            {
                NodeType = "op", Label = "Phép toán", Description = "Phép tính 2 vế: ADD/SUBTRACT/MULTIPLY/DIVIDE",
                Example = "{\"op\":\"MULTIPLY\",\"left\":{\"ref\":\"S2A_SUBTOTAL\"},\"right\":{\"literal\":0.05}}",
                Fields = new()
                {
                    new() { FieldName = "op", FieldType = "enum", Required = true, Description = "Loại phép toán", AllowedValues = new() {"ADD","SUBTRACT","MULTIPLY","DIVIDE"} },
                    new() { FieldName = "left", FieldType = "node", Required = true, Description = "Vế trái (node con)" },
                    new() { FieldName = "right", FieldType = "node", Required = true, Description = "Vế phải (node con)" }
                }
            },
            new()
            {
                NodeType = "fn", Label = "Hàm", Description = "Hàm toán học: MAX/MIN/ABS",
                Example = "{\"fn\":\"MAX\",\"args\":[{\"literal\":0},{\"ref\":\"S2A_PROFIT\"}]}",
                Fields = new()
                {
                    new() { FieldName = "fn", FieldType = "enum", Required = true, Description = "Tên hàm", AllowedValues = new() {"MAX","MIN","ABS"} },
                    new() { FieldName = "args", FieldType = "node[]", Required = true, Description = "Danh sách tham số (mảng node con)" }
                }
            },
            new()
            {
                NodeType = "context", Label = "Giá trị runtime", Description = "Giá trị từ context chạy (dùng trong foreach hoặc độc lập)",
                Example = "{\"context\":\"group_amount\"}",
                Fields = new()
                {
                    new() { FieldName = "context", FieldType = "enum", Required = true, Description = "Tên giá trị runtime",
                        AllowedValues = new() {"period_start","period_end","business_type","group_amount","group_cost","group_deduction","total_amount"} }
                }
            },
            new()
            {
                NodeType = "foreach", Label = "Lặp theo ngành", Description = "Lặp qua từng ngành nghề, tính biểu thức con cho mỗi nhóm, rồi reduce (SUM/MAX/MIN)",
                Example = "{\"foreach\":\"industry\",\"apply\":{\"op\":\"MULTIPLY\",\"left\":{\"context\":\"group_amount\"},\"right\":{\"lookup\":{\"entity\":\"IndustryTaxRates\",\"field\":\"TaxRate\",\"filter\":{\"TaxType\":\"VAT\"}}}},\"reduce\":\"SUM\"}",
                Fields = new()
                {
                    new() { FieldName = "foreach", FieldType = "enum", Required = true, Description = "Loại lặp", AllowedValues = new() {"industry"} },
                    new() { FieldName = "apply", FieldType = "node", Required = true, Description = "Biểu thức tính cho mỗi nhóm (có thể dùng context node)" },
                    new() { FieldName = "reduce", FieldType = "enum", Required = false, Description = "Phép gộp kết quả (mặc định SUM)", AllowedValues = new() {"SUM","MAX","MIN"} },
                    new() { FieldName = "threshold", FieldType = "node", Required = false, Description = "Ngưỡng: nếu total_amount ≤ ngưỡng thì kết quả = 0 (legacy)" },
                    new() { FieldName = "deduction", FieldType = "node", Required = false, Description = "Giảm trừ: {amount, target:'highest_revenue'} — trừ vào doanh thu ngành cao nhất (dùng cho PIT Cách 1)" }
                }
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
            throw new BadRequestException(MessageKeys.AdminAccEntityCodeExists, null, request.EntityCode);

        var entity = new MappableEntity
        {
            EntityCode = request.EntityCode.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Description = request.Description,
            Category = request.Category.Trim(),
            IsActive = false,
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

        if (request.EntityCode != null)
        {
            var entityCode = request.EntityCode.Trim();
            if (!string.Equals(entity.EntityCode, entityCode, StringComparison.Ordinal))
            {
                var existing = await _uow.AccountingTemplates.GetMappableEntityByCodeAsync(entityCode);
                if (existing != null && existing.EntityId != entity.EntityId)
                    throw new BadRequestException(MessageKeys.AdminAccEntityCodeExists, null, request.EntityCode);
            }

            entity.EntityCode = entityCode;
        }

        if (request.DisplayName != null)
            entity.DisplayName = request.DisplayName.Trim();
        if (request.Description != null)
            entity.Description = request.Description;
        if (request.Category != null)
            entity.Category = request.Category.Trim();
        if (request.IsActive.HasValue)
            entity.IsActive = request.IsActive.Value;

        entity.UpdatedAt = DateTime.UtcNow;

        _uow.AccountingTemplates.UpdateMappableEntity(entity);
        await _uow.SaveChangesAsync();

        return MapMappableEntity(entity);
    }

    public async Task DeleteMappableEntityAsync(int entityId)
    {
        var entity = await _uow.AccountingTemplates.GetMappableEntityWithFieldsAsync(entityId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (entity.IsActive)
            throw new BadRequestException(MessageKeys.AdminAccCannotDeleteActiveEntity);

        _uow.AccountingTemplates.RemoveMappableEntity(entity);
        await _uow.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════
    // MappableFields CRUD
    // ══════════════════════════════════════════════

    public async Task<AdminMappableFieldDto> CreateMappableFieldAsync(int entityId, CreateMappableFieldRequest request)
    {
        var entity = await _uow.AccountingTemplates.GetMappableEntityWithFieldsAsync(entityId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (entity.Fields.Any(f => f.FieldCode == request.FieldCode.Trim()))
            throw new BadRequestException(MessageKeys.AdminAccFieldCodeExistsOnEntity, null, request.FieldCode);

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

        if (request.FieldCode != null)
        {
            var fieldCode = request.FieldCode.Trim();
            if (!string.Equals(field.FieldCode, fieldCode, StringComparison.Ordinal))
            {
                var entity = await _uow.AccountingTemplates.GetMappableEntityWithFieldsAsync(field.EntityId)
                    ?? throw new NotFoundException(MessageKeys.NotFound);

                if (entity.Fields.Any(f => f.FieldId != fieldId && f.FieldCode == fieldCode))
                    throw new BadRequestException(MessageKeys.AdminAccFieldCodeExistsOnEntity, null, request.FieldCode);
            }

            field.FieldCode = fieldCode;
        }

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
            throw new BadRequestException(MessageKeys.AdminAccCannotAddRowsToActiveVersionWithBooks);

        if (!RowDefinitionConstants.RowType.All.Contains(request.RowType))
            throw new BadRequestException(MessageKeys.AdminAccInvalidRowType, null, request.RowType);

        if (!RowDefinitionConstants.Position.All.Contains(request.Position))
            throw new BadRequestException(MessageKeys.AdminAccInvalidPosition, null, request.Position);

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
                throw new BadRequestException(MessageKeys.AdminAccInvalidRowType, null, request.RowType);
            rowDef.RowType = request.RowType;
        }
        if (request.RowLabel != null) rowDef.RowLabel = request.RowLabel;
        if (request.Position != null)
        {
            if (!RowDefinitionConstants.Position.All.Contains(request.Position))
                throw new BadRequestException(MessageKeys.AdminAccInvalidPosition, null, request.Position);
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
            throw new BadRequestException(MessageKeys.AdminAccCannotDeleteRowsFromActiveVersionWithBooks);

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
            throw new BadRequestException(MessageKeys.AdminAccCannotAddMappingsToActiveVersionWithBooks);

        if (version.FieldMappings.Any(m => m.FieldCode == request.FieldCode.Trim()))
            throw new BadRequestException(MessageKeys.AdminAccFieldCodeExists, null, request.FieldCode);

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
            DependsOn = mapping.DependsOn,
            CalculationOrder = mapping.CalculationOrder,
            ExportColumn = mapping.ExportColumn,
            SortOrder = mapping.SortOrder,
            IsRequired = mapping.IsRequired
        };
    }

    public async Task DeleteFieldMappingAsync(int mappingId)
    {
        var mapping = await _uow.AccountingTemplates.GetMappingByIdAsync(mappingId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var version = mapping.TemplateVersion;
        if (version.IsActive && version.AccountingBooks.Any())
            throw new BadRequestException(MessageKeys.AdminAccCannotDeleteMappingFromActiveVersionWithBooks);

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

        var mappings = version.FieldMappings
            .OrderBy(m => m.SortOrder)
            .Select(m => new AdminTemplateFieldMappingDto
            {
                MappingId = m.MappingId,
                FieldCode = m.FieldCode,
                FieldLabel = m.FieldLabel,
                FieldType = m.FieldType,
                SourceType = m.SourceType,
                SourceEntityId = m.SourceEntityId,
                SourceEntityCode = m.SourceEntity?.EntityCode,
                SourceEntityDisplayName = m.SourceEntity?.DisplayName,
                SourceFieldId = m.SourceFieldId,
                SourceFieldCode = m.SourceField?.FieldCode,
                SourceFieldDisplayName = m.SourceField?.DisplayName,
                FilterJson = m.FilterJson,
                AggregationType = m.AggregationType,
                FormulaId = m.FormulaId,
                FormulaCode = m.Formula?.Code,
                FormulaName = m.Formula?.Name,
                FormulaExpression = m.FormulaExpression,
                DependsOn = m.DependsOn,
                CalculationOrder = m.CalculationOrder,
                ExportColumn = m.ExportColumn,
                SortOrder = m.SortOrder,
                IsRequired = m.IsRequired
            })
            .ToList();

        return new AdminFullStructureDto
        {
            TemplateVersionId = version.TemplateVersionId,
            TemplateId = version.TemplateId,
            TemplateCode = version.Template?.TemplateCode ?? string.Empty,
            TemplateName = version.Template?.Name ?? string.Empty,
            VersionLabel = version.VersionLabel,
            IsActive = version.IsActive,
            EffectiveFrom = version.EffectiveFrom,
            ChangeNotes = version.ChangeNotes,
            FieldMappings = mappings,
            Columns = new AdminColumnGroupDto
            {
                DataColumns = mappings
                    .Where(m => m.SourceType is "query" or "static" or "auto")
                    .ToList(),
                FormulaColumns = mappings
                    .Where(m => m.SourceType == "formula")
                    .ToList()
            },
            RowDefinitions = rows.Select(MapRowDefinition).ToList(),
            RenderPreview = BuildRenderPreview(rows)
        };
    }

    // ══════════════════════════════════════════════
    // BusinessTypes + IndustryTaxRates Admin
    // ══════════════════════════════════════════════

    public async Task<List<AdminBusinessTypesWithRatesDto>> GetBusinessTypesWithRatesAsync(int rulesetId)
    {
        var ruleset = await _uow.TaxRulesets.GetByIdWithRulesAsync(rulesetId)
            ?? throw new NotFoundException(MessageKeys.NotFound);
        var businessTypes = await _uow.BusinessTypes.GetAllAsync();

        return businessTypes
            .OrderBy(bt => bt.Code)
            .Select(bt => new AdminBusinessTypesWithRatesDto
            {
                BusinessTypeId = bt.BusinessTypeId,
                Code = bt.Code,
                Name = bt.Name,
                Description = bt.Description,
                Status = bt.Status,
                TaxRates = ruleset.IndustryTaxRates
                    .Where(r => r.BusinessTypeId == bt.BusinessTypeId)
                    .OrderBy(r => r.TaxType)
                    .Select(MapIndustryTaxRate)
                    .ToList()
            }).ToList();
    }

    public async Task<AdminBusinessTypeDetailDto> CreateBusinessTypeAsync(
        CreateBusinessTypeRequest request, Guid actorUserId)
    {
        var code = request.Code?.Trim() ?? "";
        if (string.IsNullOrEmpty(code))
            throw new BadRequestException(MessageKeys.AdminAccCodeRequired);

        var name = request.Name?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
            throw new BadRequestException(MessageKeys.AdminAccNameRequired);

        var existing = await _uow.BusinessTypes.GetByCodeAsync(code);
        if (existing != null)
            throw new BadRequestException(MessageKeys.AdminAccBusinessTypeCodeExists, null, code);

        var bt = new BusinessType
        {
            BusinessTypeId = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = request.Description?.Trim(),
            Status = BusinessTypeConstants.Active,
            CreatedBy = actorUserId,
            ModifiedBy = actorUserId,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        await _uow.BusinessTypes.AddAsync(bt);
        await _uow.SaveChangesAsync();

        return new AdminBusinessTypeDetailDto
        {
            BusinessTypeId = bt.BusinessTypeId,
            Code = bt.Code,
            Name = bt.Name,
            Description = bt.Description,
            Status = bt.Status
        };
    }

    public async Task<AdminBusinessTypeDetailDto> UpdateBusinessTypeAsync(
        Guid businessTypeId, UpdateBusinessTypeRequest request, Guid actorUserId)
    {
        var bt = await _uow.BusinessTypes.GetByIdAsync(businessTypeId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (request.Name != null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrEmpty(name))
                throw new BadRequestException(MessageKeys.AdminAccNameCannotBeEmpty);
            bt.Name = name;
        }

        if (request.Description != null)
            bt.Description = request.Description;

        if (request.Status != null)
        {
            if (!BusinessTypeConstants.AllowedStatuses.Contains(request.Status))
                throw new BadRequestException(
                    MessageKeys.AdminAccBusinessTypeStatusInvalid,
                    null,
                    string.Join(", ", BusinessTypeConstants.AllowedStatuses));
            bt.Status = request.Status.ToLowerInvariant();
        }

        bt.ModifiedBy = actorUserId;
        bt.LastModifiedAt = DateTime.UtcNow;
        _uow.BusinessTypes.Update(bt);
        await _uow.SaveChangesAsync();

        return new AdminBusinessTypeDetailDto
        {
            BusinessTypeId = bt.BusinessTypeId,
            Code = bt.Code,
            Name = bt.Name,
            Description = bt.Description,
            Status = bt.Status
        };
    }

    public async Task<bool> DeleteBusinessTypeAsync(Guid businessTypeId, Guid actorUserId)
    {
        var bt = await _uow.BusinessTypes.GetByIdAsync(businessTypeId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        try
        {
            _uow.BusinessTypes.Delete(bt);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex) when (IsForeignKeyDeleteConstraintViolation(ex))
        {
            bt.Status = BusinessTypeConstants.Inactive;
            bt.ModifiedBy = actorUserId;
            bt.LastModifiedAt = DateTime.UtcNow;
            _uow.BusinessTypes.Update(bt);
            await _uow.SaveChangesAsync();
            return false;
        }
    }

    private static bool IsForeignKeyDeleteConstraintViolation(Exception ex)
    {
        // MySQL FK delete violation usually includes this phrase and/or errno 1451.
        var message = ex.ToString();
        return message.Contains("foreign key constraint fails", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Cannot delete or update a parent row", StringComparison.OrdinalIgnoreCase)
            || message.Contains("1451", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<AdminIndustryTaxRateDto>> UpsertIndustryTaxRatesAsync(
        int rulesetId, Guid businessTypeId, UpsertIndustryTaxRatesRequest request)
    {
        _ = await _uow.TaxRulesets.GetByIdWithRulesAsync(rulesetId)
            ?? throw new NotFoundException(MessageKeys.NotFound);
        _ = await _uow.BusinessTypes.GetByIdAsync(businessTypeId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        var invalidTypes = request.Rates
            .Where(r => !IndustryTaxRateConstants.AllowedTaxTypes.Contains(r.TaxType))
            .Select(r => r.TaxType)
            .Distinct()
            .ToList();
        if (invalidTypes.Count > 0)
            throw new BadRequestException(
                MessageKeys.AdminAccInvalidTaxTypes,
                null,
                string.Join(", ", invalidTypes),
                string.Join(", ", IndustryTaxRateConstants.AllowedTaxTypes));

        var duplicates = request.Rates
            .GroupBy(r => r.TaxType, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
            throw new BadRequestException(
                MessageKeys.AdminAccDuplicateTaxTypes,
                null,
                string.Join(", ", duplicates));

        var outOfRange = request.Rates
            .Where(r => r.TaxRate < IndustryTaxRateConstants.MinRate || r.TaxRate > IndustryTaxRateConstants.MaxRate)
            .ToList();
        if (outOfRange.Count > 0)
            throw new BadRequestException(
                MessageKeys.AdminAccTaxRateOutOfRange,
                null,
                IndustryTaxRateConstants.MinRate,
                IndustryTaxRateConstants.MaxRate);

        List<IndustryTaxRate> savedRates = new();

        await _uow.ExecuteResilientAsync(async ct =>
        {
            var existing = await _uow.TaxRulesets.GetRatesByBusinessTypeAsync(rulesetId, businessTypeId);
            _uow.TaxRulesets.RemoveRates(existing);

            savedRates = request.Rates.Select(item => new IndustryTaxRate
            {
                RulesetId = rulesetId,
                BusinessTypeId = businessTypeId,
                TaxType = item.TaxType,
                TaxRate = item.TaxRate,
                Description = item.Description
            }).ToList();

            foreach (var rate in savedRates)
                _uow.TaxRulesets.AddRate(rate);
        });

        return savedRates.OrderBy(r => r.TaxType).Select(MapIndustryTaxRate).ToList();
    }

    public async Task<AdminTaxRulesetDto> CreateTaxRulesetAsync(
        CreateTaxRulesetRequest request, Guid actorUserId)
    {
        var code = request.Code?.Trim() ?? "";
        if (string.IsNullOrEmpty(code))
            throw new BadRequestException(MessageKeys.AdminAccCodeRequired);

        var name = request.Name?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
            throw new BadRequestException(MessageKeys.AdminAccNameRequired);

        var version = request.Version?.Trim() ?? "";
        if (string.IsNullOrEmpty(version))
            throw new BadRequestException(MessageKeys.AdminAccVersionRequired);

        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value <= request.EffectiveFrom)
            throw new BadRequestException(MessageKeys.AdminAccEffectiveToMustBeAfterEffectiveFrom);

        var ruleset = new TaxRuleset
        {
            Code = code,
            Name = name,
            Description = request.Description?.Trim(),
            Version = version,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = false,
            CreatedByUserId = actorUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.ExecuteResilientAsync(async ct =>
        {
            await _uow.TaxRulesets.AddAsync(ruleset);
            await _uow.SaveChangesAsync(ct);

            // Clone rates from source ruleset if requested
            if (request.CloneFromRulesetId.HasValue)
            {
                var source = await _uow.TaxRulesets.GetByIdWithRulesAsync(request.CloneFromRulesetId.Value)
                    ?? throw new BadRequestException(MessageKeys.AdminAccSourceRulesetNotFound, null, request.CloneFromRulesetId.Value);

                foreach (var srcRate in source.IndustryTaxRates)
                {
                    _uow.TaxRulesets.AddRate(new IndustryTaxRate
                    {
                        RulesetId = ruleset.RulesetId,
                        BusinessTypeId = srcRate.BusinessTypeId,
                        TaxType = srcRate.TaxType,
                        TaxRate = srcRate.TaxRate,
                        Description = srcRate.Description
                    });
                }
            }
        });

        // Re-fetch to populate navigation for mapper
        var created = await _uow.TaxRulesets.GetByIdWithRulesAsync(ruleset.RulesetId)
            ?? throw new NotFoundException(MessageKeys.NotFound);
        return MapTaxRuleset(created);
    }

    public async Task<AdminTaxRulesetDto> UpdateTaxRulesetAsync(
        int rulesetId, UpdateTaxRulesetRequest request)
    {
        var ruleset = await _uow.TaxRulesets.GetByIdWithRulesAsync(rulesetId)
            ?? throw new NotFoundException(MessageKeys.NotFound);

        if (request.Name != null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrEmpty(name))
                throw new BadRequestException(MessageKeys.AdminAccNameCannotBeEmpty);
            ruleset.Name = name;
        }

        if (request.Description != null)
            ruleset.Description = request.Description;

        if (request.Version != null)
        {
            var version = request.Version.Trim();
            if (string.IsNullOrEmpty(version))
                throw new BadRequestException(MessageKeys.AdminAccVersionCannotBeEmpty);
            ruleset.Version = version;
        }

        if (request.EffectiveFrom.HasValue)
            ruleset.EffectiveFrom = request.EffectiveFrom.Value;

        if (request.EffectiveTo.HasValue)
            ruleset.EffectiveTo = request.EffectiveTo.Value;

        if (ruleset.EffectiveTo.HasValue && ruleset.EffectiveTo.Value <= ruleset.EffectiveFrom)
            throw new BadRequestException(MessageKeys.AdminAccEffectiveToMustBeAfterEffectiveFrom);

        _uow.TaxRulesets.Update(ruleset);
        await _uow.SaveChangesAsync();
        return MapTaxRuleset(ruleset);
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
            FormulaName = r.Formula?.Name,
            // EffectiveFormulaExpression: the actual expression that will be evaluated.
            // FormulaExpression (testing override) takes priority; fallback to the linked formula's JSON.
            EffectiveFormulaExpression = r.Formula?.ExpressionJson,
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

    private static AdminTemplateDto MapTemplate(AccountingTemplate t)
    {
        var groups = string.IsNullOrEmpty(t.ApplicableGroups)
            ? new List<int>()
            : JsonSerializer.Deserialize<List<int>>(t.ApplicableGroups) ?? new();
        var methods = string.IsNullOrEmpty(t.ApplicableMethods)
            ? null
            : JsonSerializer.Deserialize<List<string>>(t.ApplicableMethods);
        return new AdminTemplateDto
        {
            TemplateId = t.TemplateId,
            TemplateCode = t.TemplateCode,
            Name = t.Name,
            Description = t.Description,
            DataSourceType = t.DataSourceType,
            ApplicableGroups = groups,
            ApplicableMethods = methods,
            IsActive = t.IsActive,
            Versions = t.Versions
                .OrderByDescending(v => v.CreatedAt)
                .Select(MapTemplateVersion)
                .ToList()
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

    private static AdminIndustryTaxRateDto MapIndustryTaxRate(IndustryTaxRate rate)
    {
        return new AdminIndustryTaxRateDto
        {
            RateId = rate.RateId,
            TaxType = rate.TaxType,
            TaxRate = rate.TaxRate,
            Description = rate.Description
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
            Description = formula.Description,
            Explanation = FormulaExplainer.Explain(formula.ExpressionJson)
        };
    }
}
