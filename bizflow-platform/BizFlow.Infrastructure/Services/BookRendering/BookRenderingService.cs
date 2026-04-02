using System.Text.Json;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services.BookRendering;

/// <summary>
/// Unified rendering pipeline for all 6 TT152 templates.
/// Pipeline: Load mappings → Query source data → Evaluate formulas → Compose rows.
/// </summary>
public class BookRenderingService : IBookRenderingService
{
    private readonly IUnitOfWork _uow;
    private readonly IFormulaEngine _formulaEngine;
    private readonly ILogger<BookRenderingService> _logger;

    public BookRenderingService(
        IUnitOfWork uow,
        IFormulaEngine formulaEngine,
        ILogger<BookRenderingService> logger)
    {
        _uow = uow;
        _formulaEngine = formulaEngine;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // RENDER ROWS (cursor-based, batch 200)
    // ────────────────────────────────────────────────────────
    public async Task<BookRenderResult> RenderRowsAsync(
        BookRenderContext context, string? cursor, int batchSize)
    {
        // 1. Load field mappings for this template version
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAsync(context.TemplateVersionId);
        if (version == null)
            return new BookRenderResult();

        var dataMappings = version.FieldMappings
            .Where(m => m.SourceType == "query" || m.SourceType == "auto")
            .OrderBy(m => m.SortOrder)
            .ToList();

        // 2. Query source data based on template type
        var sourceRows = await QuerySourceDataAsync(context, dataMappings, cursor, batchSize);

        // 3. Auto-increment STT
        var sttOffset = ParseCursorOffset(cursor);

        // 4. Compose output rows
        var rows = new List<Dictionary<string, object?>>();
        var rowIndex = 0;

        foreach (var sourceRow in sourceRows.Items)
        {
            var row = new Dictionary<string, object?>();
            row["lineType"] = "data";
            var currentStt = sttOffset + (++rowIndex);

            foreach (var mapping in dataMappings)
            {
                row[mapping.FieldCode] = mapping.SourceType switch
                {
                    "auto" => currentStt,
                    "query" => ExtractFieldValue(sourceRow, mapping),
                    _ => null
                };
            }

            // Attach business type identifier so client can group data rows by section.
            row["businessTypeId"] = sourceRow.Values.GetValueOrDefault("BusinessTypeId")?.ToString();

            // Attach section for per_section templates (S2c, S2e) so client can filter by section.
            if (sourceRow.Section != null)
                row["section"] = sourceRow.Section;

            rows.Add(row);
        }

        return new BookRenderResult
        {
            Rows = rows,
            HasMore = sourceRows.HasMore,
            NextCursor = sourceRows.HasMore
                ? EncodeCursor(sourceRows.Items.Last(), sttOffset + rowIndex)
                : null,
            LoadedCount = rows.Count,
            TotalEstimated = sourceRows.TotalEstimated
        };
    }

    public async Task<BookSectionsRenderResult> RenderSectionsAsync(BookRenderContext context)
    {
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAsync(context.TemplateVersionId);
        if (version == null)
            return new BookSectionsRenderResult();

        var rowDefinitions = await _uow.AccountingTemplates.GetRowDefinitionsAsync(context.TemplateVersionId);

        var columns = version.FieldMappings
            .Where(m => m.SourceType == "query" || m.SourceType == "auto")
            .OrderBy(m => m.SortOrder)
            .Select(m => new Application.DTOs.AccountingBook.BookColumnDto
            {
                FieldCode = m.FieldCode,
                Label = m.FieldLabel,
                FieldType = m.FieldType,
                ExportColumn = m.ExportColumn
            })
            .ToList();

        var (formulaValues, _, formulaBreakdowns) = await EvaluateTemplateFormulasAsync(context, version);

        // Build formula lookup: FormulaId → computed value
        var formulaIdToValue = new Dictionary<long, decimal>();
        // Build formula breakdown lookup: FormulaId → (groupKey → value)
        var formulaIdToBreakdown = new Dictionary<long, Dictionary<string, decimal>>();
        foreach (var rd in rowDefinitions.Where(r => r.FormulaId.HasValue && r.Formula != null))
        {
            if (formulaValues.TryGetValue(rd.Formula!.Code, out var val))
                formulaIdToValue[rd.FormulaId!.Value] = val;
            if (formulaBreakdowns.TryGetValue(rd.Formula!.Code, out var bd))
                formulaIdToBreakdown[rd.FormulaId!.Value] = bd;
        }

        // Load tax rates once — used for taxMetadata display
        var taxRates = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(context.RulesetId, context.BusinessTypeIds);
        var taxRateLookup = taxRates
            .GroupBy(x => NormalizeTaxType(x.TaxType))
            .ToDictionary(g => g.Key, g => g.First().TaxRate, StringComparer.OrdinalIgnoreCase);

        var perGroupDefs = rowDefinitions
            .Where(r => r.Position == RowDefinitionConstants.Position.PerGroup)
            .OrderBy(r => r.SortOrder)
            .ToList();

        var perSectionDefs = rowDefinitions
            .Where(r => r.Position == RowDefinitionConstants.Position.PerSection)
            .OrderBy(r => r.SortOrder)
            .ToList();

        var footerDefinitions = rowDefinitions
            .Where(r => r.Position == RowDefinitionConstants.Position.EndOfBook)
            .OrderBy(r => r.SortOrder)
            .ToList();

        var sections = new List<BookSectionDto>();
        var groupedTaxTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var groupIndex = 0;

        // ── Path A: per_group — repeat definitions per group key (data-driven) ──
        if (perGroupDefs.Count > 0)
        {
            var groupByField = perGroupDefs
                .Select(d => d.GroupByField)
                .FirstOrDefault(f => !string.IsNullOrWhiteSpace(f));
            var sectionType = perGroupDefs
                .Select(d => d.SectionType)
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "group";

            var (groupKeys, groupNames, groupAmounts, _) =
                await ResolveGroupDataAsync(context, groupByField);

            foreach (var groupKey in groupKeys)
            {
                groupIndex++;
                var subtotal = groupAmounts.GetValueOrDefault(groupKey);
                var groupName = groupNames.GetValueOrDefault(groupKey) ?? groupKey;

                var sectionRows = new List<Dictionary<string, object?>>();
                foreach (var rowDef in perGroupDefs)
                {
                    var row = new Dictionary<string, object?> { ["lineType"] = rowDef.RowType };

                    if (rowDef.RowType == RowDefinitionConstants.RowType.DataPlaceholder)
                    {
                        var filter = new Dictionary<string, object?>();
                        if (!string.IsNullOrWhiteSpace(groupByField))
                            filter[ToCamelCase(groupByField)] = groupKey;
                        row["dataFilter"] = filter;
                        sectionRows.Add(row);
                        continue;
                    }

                    var resolvedLabel = ResolveRowLabel(rowDef.RowLabel, groupIndex, groupName);
                    if (!string.IsNullOrWhiteSpace(resolvedLabel))
                        row["dien_giai"] = resolvedLabel;

                    if (rowDef.RowType == RowDefinitionConstants.RowType.Subtotal || rowDef.RowType == RowDefinitionConstants.RowType.SectionSubtotal)
                    {
                        var subtotalValue = ResolveFormulaValue(rowDef, formulaIdToValue) ?? subtotal;
                        row[GetValueFieldCode(rowDef)] = subtotalValue;
                        row["explanation"] = $"Tong doanh thu nhom \"{groupName}\" = {subtotalValue:#,0} VND";
                    }
                    else if (rowDef.RowType == RowDefinitionConstants.RowType.TaxLine)
                    {
                        var taxType = NormalizeTaxType(rowDef.TaxType);

                        // Use formula breakdown for per-group tax value
                        decimal taxAmount = 0m;
                        decimal? taxRate = null;
                        if (rowDef.FormulaId.HasValue
                            && formulaIdToBreakdown.TryGetValue(rowDef.FormulaId.Value, out var breakdown)
                            && breakdown.TryGetValue(groupKey, out var groupTaxValue))
                        {
                            taxAmount = groupTaxValue;
                        }
                        else
                        {
                            // Fallback: use total formula value if no breakdown
                            taxAmount = ResolveFormulaValue(rowDef, formulaIdToValue) ?? 0m;
                        }

                        // Try to get rate for metadata display
                        var rateEntry = taxRates.FirstOrDefault(r =>
                            r.BusinessTypeId.ToString() == groupKey
                            && string.Equals(NormalizeTaxType(r.TaxType), taxType, StringComparison.OrdinalIgnoreCase));
                        taxRate = rateEntry?.TaxRate;

                        row[GetValueFieldCode(rowDef)] = taxAmount;
                        row["taxMetadata"] = new Dictionary<string, object?>
                        {
                            ["taxType"] = rowDef.TaxType,
                            ["rate"] = taxRate,
                            ["source"] = "FORMULA"
                        };

                        row["explanation"] = taxRate.HasValue
                            ? $"{subtotal:#,0} x {taxRate.Value:P4} = {taxAmount:#,0}"
                            : $"Formula = {taxAmount:#,0}";

                        if (!string.IsNullOrWhiteSpace(taxType))
                            groupedTaxTotals[taxType] = groupedTaxTotals.GetValueOrDefault(taxType) + taxAmount;
                    }

                    sectionRows.Add(row);
                }

                sections.Add(new BookSectionDto
                {
                    SectionType = sectionType,
                    GroupKey = groupKey,
                    GroupName = groupName,
                    GroupIndex = groupIndex,
                    Rows = sectionRows
                });
            }
        }

        // ── Path B: per_section (S2c, S2e) — definitions define sections sequentially ──
        if (perSectionDefs.Count > 0)
        {
            var logicalSections = SplitDefinitionsByHeader(perSectionDefs);

            foreach (var (headerDef, bodyDefs) in logicalSections)
            {
                groupIndex++;
                var sectionType = headerDef?.SectionType ?? perSectionDefs.First().SectionType ?? "section";
                var sectionName = headerDef?.RowLabel ?? $"Section {groupIndex}";

                // Resolve the filter value from DB (e.g. "cash"/"bank" for cash_bank sections)
                string sectionFilterValue = headerDef?.SectionFilterValue ?? sectionName;

                var sectionRows = new List<Dictionary<string, object?>>();

                // Add header row
                if (headerDef != null)
                {
                    sectionRows.Add(new Dictionary<string, object?>
                    {
                        ["lineType"] = headerDef.RowType,
                        ["dien_giai"] = headerDef.RowLabel
                    });
                }

                foreach (var rowDef in bodyDefs)
                {
                    var row = new Dictionary<string, object?> { ["lineType"] = rowDef.RowType };

                    if (rowDef.RowType == RowDefinitionConstants.RowType.DataPlaceholder)
                    {
                        row["dataFilter"] = new Dictionary<string, object?>
                        {
                            ["businessTypeId"] = null,
                            ["section"] = sectionFilterValue
                        };
                        sectionRows.Add(row);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(rowDef.RowLabel))
                        row["dien_giai"] = rowDef.RowLabel;

                    // Resolve value from linked formula using the correct field code
                    var valueField = GetValueFieldCode(rowDef);
                    var formulaVal = ResolveFormulaValue(rowDef, formulaIdToValue);
                    if (formulaVal.HasValue)
                        row[valueField] = formulaVal.Value;

                    if (rowDef.RowType == RowDefinitionConstants.RowType.TaxLine)
                    {
                        var taxType = NormalizeTaxType(rowDef.TaxType);

                        // Use formula value (foreach formulas compute per-industry tax)
                        var taxAmount = ResolveFormulaValue(rowDef, formulaIdToValue) ?? formulaVal ?? 0m;
                        var explanation = $"Formula = {taxAmount:#,0}";

                        row[valueField] = taxAmount;
                        row["explanation"] = explanation;
                        row["taxMetadata"] = new Dictionary<string, object?>
                        {
                            ["taxType"] = rowDef.TaxType,
                            ["rate"] = taxRateLookup.GetValueOrDefault(taxType),
                            ["source"] = "FORMULA"
                        };
                        if (!string.IsNullOrWhiteSpace(taxType))
                            groupedTaxTotals[taxType] = groupedTaxTotals.GetValueOrDefault(taxType) + taxAmount;
                    }

                    sectionRows.Add(row);
                }

                sections.Add(new BookSectionDto
                {
                    SectionType = sectionType,
                    GroupKey = sectionFilterValue,
                    GroupName = sectionName,
                    GroupIndex = groupIndex,
                    Rows = sectionRows
                });
            }
        }

        // ── Footer rows (end_of_book) ──
        var footerRows = new List<Dictionary<string, object?>>();

        foreach (var rowDef in footerDefinitions)
        {
            var row = new Dictionary<string, object?>
            {
                ["lineType"] = rowDef.RowType
            };

            if (!string.IsNullOrWhiteSpace(rowDef.RowLabel))
                row["dien_giai"] = rowDef.RowLabel;

            var footerValueField = GetValueFieldCode(rowDef);
            var taxType = NormalizeTaxType(rowDef.TaxType);

            // ── tax_line in footer: use formula value ──
            if (rowDef.RowType == RowDefinitionConstants.RowType.TaxLine
                && !string.IsNullOrWhiteSpace(taxType)
                && !groupedTaxTotals.ContainsKey(taxType))
            {
                var taxAmount = ResolveFormulaValue(rowDef, formulaIdToValue) ?? 0m;
                var explanation = $"Formula = {taxAmount:#,0}";

                row[footerValueField] = taxAmount;
                row["explanation"] = explanation;
                row["taxMetadata"] = new Dictionary<string, object?>
                {
                    ["taxType"] = rowDef.TaxType,
                    ["rate"] = taxRateLookup.GetValueOrDefault(taxType),
                    ["source"] = "FORMULA"
                };

                // Build taxBreakdown from formula breakdown if available
                if (rowDef.FormulaId.HasValue
                    && formulaIdToBreakdown.TryGetValue(rowDef.FormulaId.Value, out var breakdown)
                    && breakdown.Count > 0)
                {
                    var btNames = await LoadBusinessTypeNamesAsync(context.BusinessTypeIds);
                    row["taxBreakdown"] = breakdown.Select(kv => new Dictionary<string, object?>
                    {
                        ["businessTypeId"] = kv.Key,
                        ["businessTypeName"] = Guid.TryParse(kv.Key, out var gid)
                            ? btNames.GetValueOrDefault(gid) ?? "N/A" : kv.Key,
                        ["taxAmount"] = kv.Value
                    }).ToList();
                }

                if (!string.IsNullOrWhiteSpace(taxType))
                    groupedTaxTotals[taxType] = groupedTaxTotals.GetValueOrDefault(taxType) + taxAmount;
            }
            // ── grand_total with tax type: use formula value directly, or fallback to accumulated ──
            else if (rowDef.RowType == RowDefinitionConstants.RowType.GrandTotal
                && !string.IsNullOrWhiteSpace(taxType))
            {
                var grandTotalValue = ResolveFormulaValue(rowDef, formulaIdToValue)
                    ?? groupedTaxTotals.GetValueOrDefault(taxType);
                row[footerValueField] = grandTotalValue;
                row["explanation"] = $"Tong cong = {grandTotalValue:#,0} VND";
            }
            else
            {
                // For non-tax rows: use formula value
                var formulaVal = ResolveFormulaValue(rowDef, formulaIdToValue);
                if (formulaVal.HasValue)
                {
                    row[footerValueField] = formulaVal.Value;
                }
            }

            // Add default taxMetadata if not already set for tax_line rows
            if (rowDef.RowType == RowDefinitionConstants.RowType.TaxLine && !row.ContainsKey("taxMetadata"))
            {
                row["taxMetadata"] = new Dictionary<string, object?>
                {
                    ["taxType"] = rowDef.TaxType,
                    ["rate"] = taxRateLookup.GetValueOrDefault(taxType),
                    ["source"] = "DEFAULT"
                };
            }

            footerRows.Add(row);
        }

        return new BookSectionsRenderResult
        {
            Columns = columns,
            Sections = sections,
            FooterRows = footerRows
        };
    }

    // ────────────────────────────────────────────────────────
    // COMPUTE SUMMARY (formulas → KPIs)
    // ────────────────────────────────────────────────────────
    public async Task<BookFormulaSummary> ComputeSummaryAsync(BookRenderContext context)
    {
        // 1. Load formula mappings for this template
        var version = await _uow.AccountingTemplates.GetVersionWithMappingsAsync(context.TemplateVersionId);
        if (version == null)
            return new BookFormulaSummary();

        // 2. Evaluate formulas for this template (including foreach tax formulas)
        var (results, _, _) = await EvaluateTemplateFormulasAsync(context, version);

        // 3. Build summary
        var summary = new BookFormulaSummary
        {
            FormulaValues = results
        };

        // Extract key KPIs using formula naming conventions (no template-specific switch)
        var prefix = $"{context.TemplateCode.ToUpperInvariant()}_";
        summary.TotalRevenue = FindFormulaValue(results, prefix, "_TOTAL_REVENUE", "_QUARTERLY_TOTAL");
        summary.TotalCost = FindFormulaValue(results, prefix, "_TOTAL_COST");

        // Tax is now computed by foreach formulas — sum VAT + PIT formula values
        summary.TotalTax = SumFormulaValues(results, prefix, "_VAT", "_PIT");

        // Count total rows (quick count)
        summary.TotalRows = await CountSourceRowsAsync(context);

        return summary;
    }

    private async Task<(Dictionary<string, decimal> FormulaValues, Dictionary<long, string> FormulaIdToCode, Dictionary<string, Dictionary<string, decimal>> FormulaBreakdowns)>
        EvaluateTemplateFormulasAsync(BookRenderContext context, AccountingTemplateVersion version)
    {
        var formulaIds = version.FieldMappings
            .Where(m => m.FormulaId != null)
            .Select(m => m.FormulaId!.Value)
            .Distinct()
            .ToHashSet();

        var templatePrefix = $"{context.TemplateCode.ToUpperInvariant()}_";

        // Load explicitly-referenced formulas by ID (regardless of IsActive — draft formulas must be testable)
        var explicitFormulas = formulaIds.Count > 0
            ? await _uow.FormulaDefinitions.GetByIdsAsync(formulaIds)
            : new List<FormulaDefinition>();

        // Load active prefix-matched formulas for dependency resolution (ref nodes, grand totals, etc.)
        var allActiveFormulas = await _uow.FormulaDefinitions.GetActiveAsync();
        var prefixFormulas = allActiveFormulas
            .Where(f => !formulaIds.Contains(f.FormulaId)
                        && f.Code.StartsWith(templatePrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var templateFormulas = explicitFormulas
            .Concat(prefixFormulas)
            .OrderBy(f => f.FormulaId)
            .ToList();

        if (templateFormulas.Count == 0)
            return (new Dictionary<string, decimal>(), new Dictionary<long, string>(), new Dictionary<string, Dictionary<string, decimal>>());

        var formulaCtx = new FormulaEvaluationContext
        {
            BookId = context.BookId,
            BusinessLocationId = context.BusinessLocationId,
            PeriodId = context.PeriodId,
            PeriodStart = context.PeriodStart,
            PeriodEnd = context.PeriodEnd,
            RulesetId = context.RulesetId,
            BusinessTypeIds = context.BusinessTypeIds
        };

        var evalResults = await _formulaEngine.EvaluateFormulasWithBreakdownAsync(formulaCtx, templateFormulas);
        var formulaIdToCode = templateFormulas.ToDictionary(f => f.FormulaId, f => f.Code);
        return (evalResults.Values, formulaIdToCode, evalResults.Breakdowns);
    }

    private static Dictionary<string, decimal?> BuildFormulaFieldValues(
        AccountingTemplateVersion version,
        IReadOnlyDictionary<string, decimal> formulaValues,
        IReadOnlyDictionary<long, string> formulaIdToCode)
    {
        var result = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in version.FieldMappings.Where(m => m.SourceType == "formula"))
        {
            decimal? value = null;

            if (mapping.FormulaId.HasValue
                && formulaIdToCode.TryGetValue(mapping.FormulaId.Value, out var formulaCode)
                && formulaValues.TryGetValue(formulaCode, out var formulaValue))
            {
                value = formulaValue;
            }
            else if (formulaValues.TryGetValue(mapping.FieldCode, out var byFieldCode))
            {
                value = byFieldCode;
            }

            result[mapping.FieldCode] = value;
        }

        return result;
    }

    // ────────────────────────────────────────────────────────
    // DATA QUERY LAYER — dispatch by template type
    // ────────────────────────────────────────────────────────
    private async Task<SourceDataResult> QuerySourceDataAsync(
        BookRenderContext ctx,
        List<TemplateFieldMapping> mappings,
        string? cursor, int batchSize)
    {
        // All templates primarily query from revenues/orders
        // S2c also queries costs, S2e queries GL entries
        return ctx.DataSourceType switch
        {
            "revenues" => await QueryRevenueRowsAsync(ctx, cursor, batchSize),
            "revenue_cost" => await QueryRevenueCostRowsAsync(ctx, cursor, batchSize),
            "stock_movements" => await QueryStockMovementRowsAsync(ctx, cursor, batchSize),
            "gl_entries" => await QueryGLRowsAsync(ctx, cursor, batchSize),
            _ => new SourceDataResult()
        };
    }

    private async Task<SourceDataResult> QueryRevenueRowsAsync(
        BookRenderContext ctx, string? cursor, int batchSize)
    {
        var safeBatchSize = SanitizeBatchSize(batchSize);

        var query = new Application.DTOs.Revenue.RevenueQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = 1,
            PageSize = safeBatchSize + 1 // +1 to check hasMore
        };

        // TODO: Apply cursor-based filtering (skip past cursor position)

        var (items, totalCount) = await _uow.Revenues.SearchAsync(query);
        var list = items.Where(r => r.DeletedAt == null).ToList();

        var hasMore = list.Count > safeBatchSize;
        if (hasMore) list = list.Take(safeBatchSize).ToList();

        return new SourceDataResult
        {
            Items = list.Select(r => new SourceRow
            {
                Date = r.RevenueDate,
                Id = r.RevenueId,
                Values = new Dictionary<string, object?>
                {
                    ["RevenueId"] = r.RevenueId,
                    ["RevenueDate"] = r.RevenueDate,
                    ["Description"] = r.Description,
                    ["Amount"] = r.Amount,
                    ["RevenueType"] = r.RevenueType,
                    ["MoneyChannel"] = r.MoneyChannel,
                    ["OrderId"] = r.OrderId,
                    ["BusinessTypeId"] = r.BusinessTypeId
                }
            }).ToList(),
            HasMore = hasMore,
            TotalEstimated = totalCount
        };
    }

    private async Task<SourceDataResult> QueryRevenueCostRowsAsync(
        BookRenderContext ctx, string? cursor, int batchSize)
    {
        // S2c: merge revenue + cost rows, sorted by date
        var revenueResult = await QueryRevenueRowsAsync(ctx, null, int.MaxValue);

        var costQuery = new Application.DTOs.Cost.CostQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue
        };
        var (costItems, _) = await _uow.Costs.SearchAsync(costQuery);

        var costRows = costItems.Where(c => c.DeletedAt == null).Select(c => new SourceRow
        {
            Date = c.CostDate,
            Id = c.CostId,
            Section = "cost",
            Values = new Dictionary<string, object?>
            {
                ["CostId"] = c.CostId,
                ["CostDate"] = c.CostDate,
                ["Description"] = c.Description,
                ["Amount"] = c.Amount,
                ["CostType"] = c.CostType
            }
        }).ToList();

        // Mark revenue rows with section
        foreach (var row in revenueResult.Items)
            row.Section = "revenue";

        // Merge and sort
        var merged = revenueResult.Items.Concat(costRows)
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Id)
            .ToList();

        var hasMore = merged.Count > batchSize;
        if (hasMore) merged = merged.Take(batchSize).ToList();

        return new SourceDataResult
        {
            Items = merged,
            HasMore = hasMore,
            TotalEstimated = merged.Count
        };
    }

    private async Task<SourceDataResult> QueryGLRowsAsync(
        BookRenderContext ctx, string? cursor, int batchSize)
    {
        var safeBatchSize = SanitizeBatchSize(batchSize);

        var query = new Application.DTOs.GeneralLedger.GeneralLedgerQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = 1,
            PageSize = safeBatchSize + 1
        };

        var (items, totalCount) = await _uow.GeneralLedgerEntries.SearchAsync(query);
        var list = items.Where(e => !e.IsReversal).ToList();

        var hasMore = list.Count > safeBatchSize;
        if (hasMore) list = list.Take(safeBatchSize).ToList();

        return new SourceDataResult
        {
            Items = list.Select(e => new SourceRow
            {
                Date = e.EntryDate,
                Id = e.EntryId,
                Section = e.MoneyChannel,
                Values = new Dictionary<string, object?>
                {
                    ["EntryId"] = e.EntryId,
                    ["EntryDate"] = e.EntryDate,
                    ["Description"] = e.Description,
                    ["DebitAmount"] = e.DebitAmount,
                    ["CreditAmount"] = e.CreditAmount,
                    ["TransactionType"] = e.TransactionType,
                    ["MoneyChannel"] = e.MoneyChannel
                }
            }).ToList(),
            HasMore = hasMore,
            TotalEstimated = totalCount
        };
    }

    private async Task<SourceDataResult> QueryStockMovementRowsAsync(
        BookRenderContext ctx, string? cursor, int batchSize)
    {
        var safeBatchSize = SanitizeBatchSize(batchSize);

        var allMovements = await _uow.StockMovements.GetByLocationAsync(ctx.BusinessLocationId);

        // Filter to current period
        var list = allMovements
            .Where(sm =>
            {
                var d = DateOnly.FromDateTime(sm.CreatedAt);
                return d >= ctx.PeriodStart && d <= ctx.PeriodEnd;
            })
            .OrderBy(sm => sm.CreatedAt)
            .ThenBy(sm => sm.StockMovementId)
            .ToList();

        var totalCount = list.Count;
        var hasMore = list.Count > safeBatchSize;
        if (hasMore) list = list.Take(safeBatchSize).ToList();

        return new SourceDataResult
        {
            Items = list.Select(sm => new SourceRow
            {
                Date = DateOnly.FromDateTime(sm.CreatedAt),
                Id = sm.StockMovementId,
                Section = sm.ProductId.ToString(),  // group by ProductId
                Values = new Dictionary<string, object?>
                {
                    ["StockMovementId"] = sm.StockMovementId,
                    ["ProductId"] = sm.ProductId,
                    ["ProductName"] = sm.Product?.ProductName,
                    ["MovementDate"] = DateOnly.FromDateTime(sm.CreatedAt),
                    ["MovementType"] = sm.MovementType,
                    ["Description"] = sm.Memo ?? $"{sm.MovementType} — {sm.ReferenceType} #{sm.ReferenceId}",
                    ["Unit"] = sm.Product?.Unit,
                    ["CostPrice"] = sm.Product?.CostPrice ?? 0m,
                    ["Quantity"] = sm.Quantity,
                    ["QuantityAbs"] = Math.Abs(sm.Quantity),
                    ["ImportQty"] = sm.Quantity > 0 ? sm.Quantity : (int?)null,
                    ["ImportValue"] = sm.Quantity > 0 ? Math.Abs(sm.Quantity) * (sm.Product?.CostPrice ?? 0m) : (decimal?)null,
                    ["ExportQty"] = sm.Quantity < 0 ? Math.Abs(sm.Quantity) : (int?)null,
                    ["ExportValue"] = sm.Quantity < 0 ? Math.Abs(sm.Quantity) * (sm.Product?.CostPrice ?? 0m) : (decimal?)null,
                    ["BalanceAfter"] = sm.BalanceAfter,
                    ["BalanceValue"] = sm.BalanceAfter * (sm.Product?.CostPrice ?? 0m),
                    ["ReferenceType"] = sm.ReferenceType,
                    ["ReferenceId"] = sm.ReferenceId
                }
            }).ToList(),
            HasMore = hasMore,
            TotalEstimated = totalCount
        };
    }

    private async Task<int> CountSourceRowsAsync(BookRenderContext ctx)
    {
        return ctx.DataSourceType switch
        {
            "revenues" => (await QueryRevenueRowsAsync(ctx, null, 1)).TotalEstimated ?? 0,
            "gl_entries" => (await QueryGLRowsAsync(ctx, null, 1)).TotalEstimated ?? 0,
            "stock_movements" => (await QueryStockMovementRowsAsync(ctx, null, 1)).TotalEstimated ?? 0,
            _ => 0
        };
    }

    // ────────────────────────────────────────────────────────
    // FIELD EXTRACTION — map SourceRow → column value
    // ────────────────────────────────────────────────────────
    private static object? ExtractFieldValue(SourceRow row, TemplateFieldMapping mapping)
    {
        if (!string.IsNullOrWhiteSpace(mapping.SourceField?.FieldCode))
        {
            var sourceFieldValue = ExtractBySourceFieldCode(row, mapping.SourceField.FieldCode);
            if (sourceFieldValue != null)
                return sourceFieldValue;
        }

        // Map field code to source data
        // FieldCode in mapping maps to specific source entity fields
        return mapping.FieldCode switch
        {
            // Common
            "date" or "ngay_thang" or "ngay" => row.Date,
            "description" or "dien_giai" => row.Values.GetValueOrDefault("Description"),
            "so_hieu" => row.Values.GetValueOrDefault("OrderId")?.ToString()
                ?? row.Values.GetValueOrDefault("EntryId")?.ToString()
                ?? row.Values.GetValueOrDefault("StockMovementId")?.ToString()
                ?? row.Values.GetValueOrDefault("CostId")?.ToString(),

            // Revenue/Order
            "revenue" or "so_tien" => row.Values.GetValueOrDefault("Amount"),

            // GL Entries (S2e)
            "thu_vao" => row.Values.GetValueOrDefault("DebitAmount"),
            "chi_ra" => row.Values.GetValueOrDefault("CreditAmount"),

            // Stock Movements (S2d)
            "dvt" => row.Values.GetValueOrDefault("Unit"),
            "don_gia" => row.Values.GetValueOrDefault("CostPrice"),
            "sl_nhap" => row.Values.GetValueOrDefault("ImportQty"),
            "tien_nhap" => row.Values.GetValueOrDefault("ImportValue"),
            "sl_xuat" => row.Values.GetValueOrDefault("ExportQty"),
            "tien_xuat" => row.Values.GetValueOrDefault("ExportValue"),
            "sl_ton" => row.Values.GetValueOrDefault("BalanceAfter"),
            "tien_ton" => row.Values.GetValueOrDefault("BalanceValue"),

            // Section (S2c, S2e, S2d per product)
            "section" => row.Section,

            // Fallback: try direct match
            _ => row.Values.GetValueOrDefault(mapping.FieldCode)
        };
    }

    private static object? ExtractBySourceFieldCode(SourceRow row, string sourceFieldCode)
    {
        return sourceFieldCode switch
        {
            // Revenue/Order
            "RevenueId" => row.Values.GetValueOrDefault("RevenueId"),
            "RevenueDate" or "CompletedAt" => row.Values.GetValueOrDefault("RevenueDate") ?? row.Date,
            "Amount" or "TotalAmount" or "CostAmount" => row.Values.GetValueOrDefault("Amount"),
            "Description" or "CostDescription" => row.Values.GetValueOrDefault("Description"),
            "OrderId" or "OrderCode" => row.Values.GetValueOrDefault("OrderId"),
            "RevenueType" => row.Values.GetValueOrDefault("RevenueType"),
            "BusinessTypeId" => row.Values.GetValueOrDefault("BusinessTypeId"),

            // Cost
            "CostDate" => row.Values.GetValueOrDefault("CostDate") ?? row.Date,
            "CostType" or "CostCategory" => row.Values.GetValueOrDefault("CostType"),

            // GL
            "EntryId" => row.Values.GetValueOrDefault("EntryId"),
            "EntryDate" => row.Values.GetValueOrDefault("EntryDate") ?? row.Date,
            "DebitAmount" => row.Values.GetValueOrDefault("DebitAmount"),
            "CreditAmount" => row.Values.GetValueOrDefault("CreditAmount"),
            "TransactionType" => row.Values.GetValueOrDefault("TransactionType"),
            "MoneyChannel" => row.Values.GetValueOrDefault("MoneyChannel"),

            // Stock Movements
            "StockMovementId" => row.Values.GetValueOrDefault("StockMovementId"),
            "ProductId" => row.Values.GetValueOrDefault("ProductId"),
            "ProductName" => row.Values.GetValueOrDefault("ProductName"),
            "MovementType" => row.Values.GetValueOrDefault("MovementType"),
            "Unit" => row.Values.GetValueOrDefault("Unit"),
            "CostPrice" => row.Values.GetValueOrDefault("CostPrice"),
            "Quantity" or "QuantityDelta" => row.Values.GetValueOrDefault("Quantity"),
            "ImportQty" => row.Values.GetValueOrDefault("ImportQty"),
            "ImportValue" => row.Values.GetValueOrDefault("ImportValue"),
            "ExportQty" => row.Values.GetValueOrDefault("ExportQty"),
            "ExportValue" => row.Values.GetValueOrDefault("ExportValue"),
            "BalanceAfter" => row.Values.GetValueOrDefault("BalanceAfter"),
            "BalanceValue" => row.Values.GetValueOrDefault("BalanceValue"),

            // Fallback direct lookup by metadata field code
            _ => row.Values.GetValueOrDefault(sourceFieldCode)
        };
    }

    // ────────────────────────────────────────────────────────
    // CURSOR HELPERS
    // ────────────────────────────────────────────────────────
    private static int ParseCursorOffset(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return 0;
        var parts = cursor.Split('_');
        return parts.Length >= 2 && int.TryParse(parts[^1], out var offset) ? offset : 0;
    }

    private static int SanitizeBatchSize(int batchSize)
    {
        if (batchSize < 1)
            return 1;
        return Math.Min(batchSize, int.MaxValue - 1);
    }

    private static string EncodeCursor(SourceRow lastRow, int currentOffset)
    {
        return $"{lastRow.Date:yyyy-MM-dd}_{lastRow.Id}_{currentOffset}";
    }


    /// <summary>
    /// Resolve the output field code for a formula value from VisibleFieldCodes.
    /// Returns the first non-dien_giai field, or "so_tien" as fallback.
    /// </summary>
    private static string GetValueFieldCode(TemplateRowDefinition rowDef, string fallback = "so_tien")
    {
        if (string.IsNullOrWhiteSpace(rowDef.VisibleFieldCodes))
            return fallback;
        try
        {
            var fields = JsonSerializer.Deserialize<List<string>>(rowDef.VisibleFieldCodes);
            return fields?.FirstOrDefault(f => !string.Equals(f, "dien_giai", StringComparison.OrdinalIgnoreCase)) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private async Task<Dictionary<Guid, string>> LoadBusinessTypeNamesAsync(IEnumerable<Guid> businessTypeIds)
    {
        var ids = businessTypeIds.ToHashSet();
        var allBusinessTypes = await _uow.BusinessTypes.GetAllAsync();
        return allBusinessTypes
            .Where(x => ids.Contains(x.BusinessTypeId))
            .ToDictionary(x => x.BusinessTypeId, x => x.Name);
    }

    private async Task<Dictionary<Guid, decimal>> LoadRevenueByBusinessTypeAsync(BookRenderContext context)
    {
        var query = new Application.DTOs.Revenue.RevenueQueryParams
        {
            BusinessLocationId = context.BusinessLocationId,
            FromDate = context.PeriodStart,
            ToDate = context.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (items, _) = await _uow.Revenues.SearchAsync(query);
        return items
            .Where(r => r.DeletedAt == null && r.BusinessTypeId.HasValue)
            .GroupBy(r => r.BusinessTypeId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
    }

    private static string ResolveRowLabel(string? label, int groupIndex, string groupName)
    {
        if (string.IsNullOrWhiteSpace(label))
            return string.Empty;

        return label
            .Replace("{groupIndex}", groupIndex.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{businessTypeName}", groupName, StringComparison.OrdinalIgnoreCase)
            .Replace("{groupName}", groupName, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTaxType(string? taxType)
    {
        if (string.IsNullOrWhiteSpace(taxType))
            return string.Empty;

        if (taxType.Contains("VAT", StringComparison.OrdinalIgnoreCase))
            return RowDefinitionConstants.TaxType.Vat;
        if (taxType.Contains("PIT", StringComparison.OrdinalIgnoreCase))
            return RowDefinitionConstants.TaxType.Pit;

        return taxType;
    }

    /// <summary>
    /// Resolve group keys, names, amounts, and tax rates for per_group rendering.
    /// Extension point: add new GroupByField values here when introducing new templates.
    /// </summary>
    private async Task<(List<string> GroupKeys, Dictionary<string, string> GroupNames,
        Dictionary<string, decimal> GroupAmounts, Dictionary<(string, string), decimal> TaxRates)>
        ResolveGroupDataAsync(BookRenderContext context, string? groupByField)
    {
        if (string.IsNullOrWhiteSpace(groupByField))
        {
            // No grouping — single flat section (e.g., S1a)
            return (new List<string> { "" },
                new Dictionary<string, string>(),
                new Dictionary<string, decimal>(),
                new Dictionary<(string, string), decimal>());
        }

        if (groupByField.Equals("BusinessTypeId", StringComparison.OrdinalIgnoreCase))
        {
            var names = await LoadBusinessTypeNamesAsync(context.BusinessTypeIds);
            var amounts = await LoadRevenueByBusinessTypeAsync(context);
            var taxRates = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(
                context.RulesetId, context.BusinessTypeIds);

            var keys = context.BusinessTypeIds
                .OrderBy(id => names.GetValueOrDefault(id) ?? id.ToString())
                .Select(id => id.ToString())
                .ToList();

            return (keys,
                names.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
                amounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
                taxRates
                    .GroupBy(x => (x.BusinessTypeId.ToString(), NormalizeTaxType(x.TaxType)))
                    .ToDictionary(g => g.Key, g => g.First().TaxRate));
        }

        if (groupByField.Equals("ProductId", StringComparison.OrdinalIgnoreCase))
        {
            var allMovements = await _uow.StockMovements.GetByLocationAsync(context.BusinessLocationId);
            var periodMovements = allMovements.Where(sm =>
            {
                var d = DateOnly.FromDateTime(sm.CreatedAt);
                return d >= context.PeriodStart && d <= context.PeriodEnd;
            }).ToList();

            // Discover distinct products that have movements in this period
            var productGroups = periodMovements
                .GroupBy(sm => sm.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var keys = productGroups.Keys.OrderBy(k => k).Select(k => k.ToString()).ToList();
            var names = productGroups.ToDictionary(
                kv => kv.Key.ToString(),
                kv => kv.Value.First().Product?.ProductName ?? $"Product #{kv.Key}");
            var amounts = productGroups.ToDictionary(
                kv => kv.Key.ToString(),
                kv => kv.Value.Where(sm => sm.Quantity > 0).Sum(sm => Math.Abs(sm.Quantity) * (sm.Product?.CostPrice ?? 0m)));

            return (keys, names, amounts, new Dictionary<(string, string), decimal>());
        }

        // Future: add new GroupByField values here when introducing new templates.
        _logger.LogWarning("Unsupported GroupByField: {GroupByField}", groupByField);
        return (new List<string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, decimal>(),
            new Dictionary<(string, string), decimal>());
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private static decimal? FindFormulaValue(
        IReadOnlyDictionary<string, decimal> results, string prefix, params string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            var match = results.FirstOrDefault(kv =>
                kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                kv.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(match.Key))
                return match.Value;
        }
        return null;
    }

    private static decimal SumFormulaValues(
        IReadOnlyDictionary<string, decimal> results, string prefix, params string[] suffixes)
    {
        return results
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                         && suffixes.Any(s => kv.Key.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
            .Sum(kv => kv.Value);
    }

    /// <summary>
    /// Resolve a formula value for a row definition that has a linked FormulaId.
    /// Returns null if no formula is linked or the value is not found.
    /// </summary>
    private static decimal? ResolveFormulaValue(
        TemplateRowDefinition rowDef,
        IReadOnlyDictionary<long, decimal> formulaIdToValue)
    {
        if (!rowDef.FormulaId.HasValue)
            return null;

        return formulaIdToValue.TryGetValue(rowDef.FormulaId.Value, out var val) ? val : null;
    }

    /// <summary>
    /// Split per_section definitions into logical sections delimited by section_header rows.
    /// Each tuple is (header definition or null, body definitions).
    /// </summary>
    private static List<(TemplateRowDefinition? Header, List<TemplateRowDefinition> Body)> SplitDefinitionsByHeader(
        List<TemplateRowDefinition> definitions)
    {
        var result = new List<(TemplateRowDefinition?, List<TemplateRowDefinition>)>();
        TemplateRowDefinition? currentHeader = null;
        var currentBody = new List<TemplateRowDefinition>();

        foreach (var def in definitions)
        {
            if (def.RowType == RowDefinitionConstants.RowType.SectionHeader ||
                def.RowType == RowDefinitionConstants.RowType.IndustryHeader)
            {
                // Save previous section if any body rows exist
                if (currentHeader != null || currentBody.Count > 0)
                    result.Add((currentHeader, currentBody));

                currentHeader = def;
                currentBody = new List<TemplateRowDefinition>();
            }
            else
            {
                currentBody.Add(def);
            }
        }

        // Final section
        if (currentHeader != null || currentBody.Count > 0)
            result.Add((currentHeader, currentBody));

        return result;
    }
}

// ────────────────────────────────────────────────────────
// Internal models
// ────────────────────────────────────────────────────────
internal class SourceDataResult
{
    public List<SourceRow> Items { get; set; } = new();
    public bool HasMore { get; set; }
    public int? TotalEstimated { get; set; }
}

internal class SourceRow
{
    public DateOnly Date { get; set; }
    public long Id { get; set; }
    public string? Section { get; set; }
    public Dictionary<string, object?> Values { get; set; } = new();
}
