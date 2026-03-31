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

        var (formulaValues, _) = await EvaluateTemplateFormulasAsync(context, version);

        // Build formula lookup: FormulaId → computed value
        var formulaIdToValue = new Dictionary<long, decimal>();
        foreach (var rd in rowDefinitions.Where(r => r.FormulaId.HasValue && r.Formula != null))
        {
            if (formulaValues.TryGetValue(rd.Formula!.Code, out var val))
                formulaIdToValue[rd.FormulaId!.Value] = val;
        }

        // Load tax rates once — used by both per_group sections and footer
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

        // ── Tax rates per business type (for per-group tax computation) ──
        var perBtTaxRates = taxRates
            .GroupBy(x => (x.BusinessTypeId.ToString(), NormalizeTaxType(x.TaxType)))
            .ToDictionary(g => g.Key, g => g.First().TaxRate);

        // ── PIT threshold: Method 1 (S2a) requires total location revenue > 500M ──
        // Compute per-group PIT for both methods without relying on formula definitions.
        var isPitMethod1 = string.Equals(context.TaxMethod, "method_1", StringComparison.OrdinalIgnoreCase);

        // ── Path A: per_group — repeat definitions per group key (data-driven) ──
        if (perGroupDefs.Count > 0)
        {
            var groupByField = perGroupDefs
                .Select(d => d.GroupByField)
                .FirstOrDefault(f => !string.IsNullOrWhiteSpace(f));
            var sectionType = perGroupDefs
                .Select(d => d.SectionType)
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "group";

            var (groupKeys, groupNames, groupAmounts, perGroupTaxRates) =
                await ResolveGroupDataAsync(context, groupByField);

            // Compute total revenue across all groups (for PIT proration)
            var totalAllGroupsRevenue = groupKeys.Sum(k => groupAmounts.GetValueOrDefault(k));

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
                        var taxRate = perGroupTaxRates.GetValueOrDefault((groupKey, taxType));
                        var taxAmount = subtotal * taxRate;

                        // PIT Method 1 (S2a): threshold = 500M VND on total location revenue.
                        // If below threshold, PIT = 0 for all groups.
                        // If above threshold, PIT = group_revenue × PIT_rate (normal calculation).
                        const decimal PIT_THRESHOLD = 500_000_000m;
                        if (taxType == RowDefinitionConstants.TaxType.Pit && isPitMethod1
                            && totalAllGroupsRevenue <= PIT_THRESHOLD)
                        {
                            taxAmount = 0m;
                        }

                        row[GetValueFieldCode(rowDef)] = taxAmount;
                        row["taxMetadata"] = new Dictionary<string, object?>
                        {
                            ["taxType"] = rowDef.TaxType,
                            ["rate"] = taxRate,
                            ["source"] = "RENDERER"
                        };

                        // Build explanation for tax calculation
                        if (taxType == RowDefinitionConstants.TaxType.Pit && isPitMethod1
                            && totalAllGroupsRevenue <= PIT_THRESHOLD)
                        {
                            row["explanation"] = $"Tong DT toan location ({totalAllGroupsRevenue:#,0} VND) chua vuot nguong {PIT_THRESHOLD:#,0} → Thue TNCN = 0";
                        }
                        else
                        {
                            row["explanation"] = $"{subtotal:#,0} x {taxRate:P4} = {taxAmount:#,0}";
                        }

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

        // ── Preload revenue/cost by business type if ANY section/footer has TaxLine ──
        // Needed by both Path B (per_section) and Footer (end_of_book) for per-industry tax.
        Dictionary<Guid, decimal>? revenueByBt = null;
        Dictionary<Guid, decimal>? costByBt = null;
        decimal totalRevenueSec = 0m;
        var hasTaxLineAnywhere = perSectionDefs.Any(d => d.RowType == RowDefinitionConstants.RowType.TaxLine)
                              || footerDefinitions.Any(d => d.RowType == RowDefinitionConstants.RowType.TaxLine);
        if (hasTaxLineAnywhere)
        {
            revenueByBt = await LoadRevenueByBusinessTypeAsync(context);
            costByBt = await LoadCostByBusinessTypeAsync(context);
            totalRevenueSec = revenueByBt.Values.Sum();
        }

        // ── Path B: per_section (S2c, S2e) — definitions define sections sequentially ──
        if (perSectionDefs.Count > 0)
        {

            var templatePrefix = context.TemplateCode.ToUpperInvariant() + "_";
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
                        decimal taxAmount;
                        string explanation;

                        // Renderer computes tax per-industry (no formula dependency).
                        if (taxType == RowDefinitionConstants.TaxType.Pit)
                        {
                            // PIT Cách 2: Σ MAX(0, revenue_i - cost_i) × PIT_rate_i
                            // Uses actual cost-per-industry when available, falls back to proration.
                            var profitKey = formulaValues.Keys
                                .FirstOrDefault(k => k.StartsWith(templatePrefix, StringComparison.OrdinalIgnoreCase)
                                                  && k.Contains("PROFIT", StringComparison.OrdinalIgnoreCase));
                            var profit = profitKey != null ? formulaValues[profitKey] : 0m;
                            var totalCost = totalRevenueSec - profit;

                            taxAmount = ComputePerIndustryPitMethod2(totalRevenueSec, totalCost, revenueByBt, perBtTaxRates, costByBt);
                            var taggedCost = costByBt?.Values.Sum() ?? 0m;
                            var hasTagged = taggedCost > 0m;
                            explanation = hasTagged
                                ? $"SUM per nganh: MAX(0, DT_i - CP_i) x PIT_rate_i = {taxAmount:#,0} (CP thuc te theo nganh)"
                                : $"SUM per nganh: MAX(0, DT_i - CP_i) x PIT_rate_i = {taxAmount:#,0} (CP phan bo theo ty trong DT)";
                        }
                        else if (taxType == RowDefinitionConstants.TaxType.Vat)
                        {
                            // VAT: Σ(revenue_i × VAT_rate_i)
                            taxAmount = ComputePerIndustryVat(revenueByBt, perBtTaxRates);
                            explanation = $"SUM per nganh: DT_i x VAT_rate_i = {taxAmount:#,0}";
                        }
                        else
                        {
                            // Fallback for unknown tax types: use formula if still linked
                            taxAmount = formulaVal ?? 0m;
                            explanation = $"Formula value = {taxAmount:#,0}";
                        }

                        row[valueField] = taxAmount;
                        row["explanation"] = explanation;
                        row["taxMetadata"] = new Dictionary<string, object?>
                        {
                            ["taxType"] = rowDef.TaxType,
                            ["rate"] = taxRateLookup.GetValueOrDefault(taxType),
                            ["source"] = "RENDERER"
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
        Dictionary<Guid, string>? btNames = null; // lazy-loaded for taxBreakdown

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

            // ── tax_line in footer: compute per-industry tax (S2c PIT lives here) ──
            if (rowDef.RowType == RowDefinitionConstants.RowType.TaxLine
                && !string.IsNullOrWhiteSpace(taxType)
                && !groupedTaxTotals.ContainsKey(taxType))
            {
                var templatePrefix = context.TemplateCode.ToUpperInvariant() + "_";
                decimal taxAmount;
                string explanation;
                List<TaxBreakdownDetail>? breakdownDetails = null;

                if (taxType == RowDefinitionConstants.TaxType.Pit)
                {
                    if (isPitMethod1)
                    {
                        taxAmount = ComputePerIndustryPitMethod1(totalRevenueSec, revenueByBt, perBtTaxRates);
                        explanation = totalRevenueSec <= 500_000_000m
                            ? $"Tong DT ({totalRevenueSec:#,0}) chua vuot 500,000,000 → Thue TNCN = 0"
                            : $"SUM per nganh: DT_i x PIT_rate_i = {taxAmount:#,0}";
                    }
                    else
                    {
                        // PIT Method 2 (S2c): Σ MAX(0, revenue_i - cost_i) × PIT_rate_i
                        var profitKey = formulaValues.Keys
                            .FirstOrDefault(k => k.StartsWith(templatePrefix, StringComparison.OrdinalIgnoreCase)
                                              && k.Contains("PROFIT", StringComparison.OrdinalIgnoreCase));
                        var profit = profitKey != null ? formulaValues[profitKey] : 0m;
                        var totalCost = totalRevenueSec - profit;

                        (taxAmount, breakdownDetails) = ComputePerIndustryPitMethod2WithBreakdown(
                            totalRevenueSec, totalCost, revenueByBt, perBtTaxRates, costByBt);

                        var taggedCost = costByBt?.Values.Sum() ?? 0m;
                        explanation = taggedCost > 0m
                            ? $"SUM per nganh: MAX(0, DT_i - CP_i) x PIT_rate_i = {taxAmount:#,0} (CP thuc te theo nganh)"
                            : $"SUM per nganh: MAX(0, DT_i - CP_i) x PIT_rate_i = {taxAmount:#,0} (CP phan bo theo ty trong DT)";
                    }
                }
                else if (taxType == RowDefinitionConstants.TaxType.Vat)
                {
                    taxAmount = ComputePerIndustryVat(revenueByBt, perBtTaxRates);
                    explanation = $"SUM per nganh: DT_i x VAT_rate_i = {taxAmount:#,0}";
                }
                else
                {
                    taxAmount = ResolveFormulaValue(rowDef, formulaIdToValue) ?? 0m;
                    explanation = $"Formula value = {taxAmount:#,0}";
                }

                row[footerValueField] = taxAmount;
                row["explanation"] = explanation;
                row["taxMetadata"] = new Dictionary<string, object?>
                {
                    ["taxType"] = rowDef.TaxType,
                    ["rate"] = taxRateLookup.GetValueOrDefault(taxType),
                    ["source"] = "RENDERER"
                };

                // Build taxBreakdown array for per-industry detail
                if (breakdownDetails != null && breakdownDetails.Count > 0)
                {
                    btNames ??= await LoadBusinessTypeNamesAsync(context.BusinessTypeIds);
                    row["taxBreakdown"] = breakdownDetails.Select(d => new Dictionary<string, object?>
                    {
                        ["businessTypeId"] = d.BusinessTypeId.ToString(),
                        ["businessTypeName"] = btNames.GetValueOrDefault(d.BusinessTypeId) ?? "N/A",
                        ["revenue"] = d.Revenue,
                        ["cost"] = d.Cost,
                        ["profit"] = d.Profit,
                        ["taxRate"] = d.TaxRate,
                        ["taxAmount"] = d.TaxAmount,
                        ["explanation"] = d.Explanation
                    }).ToList();
                }

                if (!string.IsNullOrWhiteSpace(taxType))
                    groupedTaxTotals[taxType] = groupedTaxTotals.GetValueOrDefault(taxType) + taxAmount;
            }
            // ── grand_total with tax type: use accumulated per-group totals ──
            else if (rowDef.RowType == RowDefinitionConstants.RowType.GrandTotal
                && !string.IsNullOrWhiteSpace(taxType)
                && groupedTaxTotals.ContainsKey(taxType))
            {
                var groupedValue = groupedTaxTotals[taxType];
                row[footerValueField] = groupedValue;
                row["explanation"] = $"Tong cong tu cac nhom nganh = {groupedValue:#,0} VND";
            }
            else
            {
                // For non-tax rows: use formula value or fallback
                var formulaVal = ResolveFormulaValue(rowDef, formulaIdToValue);
                if (formulaVal.HasValue)
                {
                    row[footerValueField] = formulaVal.Value;
                }
                else if (rowDef.RowType == RowDefinitionConstants.RowType.GrandTotal)
                {
                    row[footerValueField] = groupedTaxTotals.GetValueOrDefault(taxType);
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

        // 2. Evaluate formulas for this template
        var (results, _) = await EvaluateTemplateFormulasAsync(context, version);

        // 3. Build summary
        var summary = new BookFormulaSummary
        {
            FormulaValues = results
        };

        // Extract key KPIs using formula naming conventions (no template-specific switch)
        var prefix = $"{context.TemplateCode.ToUpperInvariant()}_";
        summary.TotalRevenue = FindFormulaValue(results, prefix, "_TOTAL_REVENUE", "_QUARTERLY_TOTAL");
        summary.TotalCost = FindFormulaValue(results, prefix, "_TOTAL_COST");

        // Compute totalTax from per-group tax rates (not from formula, which uses a single rate).
        // This is consistent with how RenderSectionsAsync computes footer grand totals.
        summary.TotalTax = await ComputeTotalTaxFromGroupsAsync(context, results, prefix);

        // Count total rows (quick count)
        summary.TotalRows = await CountSourceRowsAsync(context);

        return summary;
    }

    private async Task<(Dictionary<string, decimal> FormulaValues, Dictionary<long, string> FormulaIdToCode)>
        EvaluateTemplateFormulasAsync(BookRenderContext context, AccountingTemplateVersion version)
    {
        var formulaIds = version.FieldMappings
            .Where(m => m.FormulaId != null)
            .Select(m => m.FormulaId!.Value)
            .Distinct()
            .ToHashSet();

        var templatePrefix = $"{context.TemplateCode.ToUpperInvariant()}_";
        var allFormulas = await _uow.FormulaDefinitions.GetActiveAsync();
        var templateFormulas = allFormulas
            .Where(f => formulaIds.Contains(f.FormulaId)
                        || f.Code.StartsWith(templatePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.FormulaId)
            .ToList();

        if (templateFormulas.Count == 0)
            return (new Dictionary<string, decimal>(), new Dictionary<long, string>());

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

        var formulaValues = await _formulaEngine.EvaluateFormulasAsync(formulaCtx, templateFormulas);
        var formulaIdToCode = templateFormulas.ToDictionary(f => f.FormulaId, f => f.Code);
        return (formulaValues, formulaIdToCode);
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
            "stock_movements" => new SourceDataResult(), // Phase B.2
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

    private async Task<int> CountSourceRowsAsync(BookRenderContext ctx)
    {
        return ctx.DataSourceType switch
        {
            "revenues" => (await QueryRevenueRowsAsync(ctx, null, 1)).TotalEstimated ?? 0,
            "gl_entries" => (await QueryGLRowsAsync(ctx, null, 1)).TotalEstimated ?? 0,
            _ => 0
        };
    }

    /// <summary>
    /// Compute total tax by summing per-group (per-industry) tax amounts.
    /// This correctly handles multi-industry books where each industry has different tax rates.
    /// PIT uses threshold logic for Method 1, weighted profit rate for Method 2.
    /// </summary>
    private async Task<decimal> ComputeTotalTaxFromGroupsAsync(
        BookRenderContext context,
        IReadOnlyDictionary<string, decimal> formulaValues,
        string templatePrefix)
    {
        var amounts = await LoadRevenueByBusinessTypeAsync(context);
        var taxRates = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(
            context.RulesetId, context.BusinessTypeIds);

        var taxRatesByBt = taxRates
            .GroupBy(x => (x.BusinessTypeId.ToString(), NormalizeTaxType(x.TaxType)))
            .ToDictionary(g => g.Key, g => g.First().TaxRate);

        var totalRevenue = amounts.Values.Sum();

        // VAT: Σ(revenue_i × VAT_rate_i)
        decimal totalVat = ComputePerIndustryVat(amounts, taxRatesByBt);

        // PIT: depends on method
        decimal totalPit;
        var isPitMethod1 = string.Equals(context.TaxMethod, "method_1", StringComparison.OrdinalIgnoreCase);

        if (isPitMethod1)
        {
            // Method 1 (S2a): revenue × PIT_rate, only if total > 500M
            totalPit = ComputePerIndustryPitMethod1(totalRevenue, amounts, taxRatesByBt);
        }
        else
        {
            // Method 2 (S2c): Σ MAX(0, revenue_i - cost_i) × PIT_rate_i
            var profitKey = formulaValues.Keys
                .FirstOrDefault(k => k.StartsWith(templatePrefix, StringComparison.OrdinalIgnoreCase)
                                  && k.Contains("PROFIT", StringComparison.OrdinalIgnoreCase));
            var profit = profitKey != null ? formulaValues[profitKey] : 0m;
            var totalCost = totalRevenue - profit;
            var costByBt = await LoadCostByBusinessTypeAsync(context);
            totalPit = ComputePerIndustryPitMethod2(totalRevenue, totalCost, amounts, taxRatesByBt, costByBt);
        }

        return totalVat + totalPit;
    }

    /// <summary>
    /// Compute VAT per-industry: Σ(revenue_i × VAT_rate_i).
    /// </summary>
    private static decimal ComputePerIndustryVat(
        IReadOnlyDictionary<Guid, decimal>? revenueByBt,
        IReadOnlyDictionary<(string, string), decimal> perBtTaxRates)
    {
        if (revenueByBt == null) return 0m;

        decimal total = 0m;
        foreach (var (btId, revenue) in revenueByBt)
        {
            var rate = perBtTaxRates.GetValueOrDefault((btId.ToString(), RowDefinitionConstants.TaxType.Vat));
            total += revenue * rate;
        }
        return Math.Round(total, 0);
    }

    /// <summary>
    /// Compute PIT per-industry for Cách 2 (S2c):
    ///   Σ MAX(0, revenue_i - cost_i) × PIT_rate_i
    /// Uses actual cost-per-industry when costs have BusinessTypeId.
    /// Falls back to prorating total cost by revenue share for legacy rows without BusinessTypeId.
    /// </summary>
    private static decimal ComputePerIndustryPitMethod2(
        decimal totalRevenue,
        decimal totalCost,
        IReadOnlyDictionary<Guid, decimal>? revenueByBt,
        IReadOnlyDictionary<(string, string), decimal> perBtTaxRates,
        IReadOnlyDictionary<Guid, decimal>? costByBt = null)
    {
        if (revenueByBt == null || totalRevenue <= 0m) return 0m;

        // Costs that have BusinessTypeId (actual per-industry)
        var taggedCost = costByBt?.Values.Sum() ?? 0m;
        // Costs without BusinessTypeId (need proration)
        var untaggedCost = Math.Max(0m, totalCost - taggedCost);

        decimal total = 0m;
        foreach (var (btId, revenue) in revenueByBt)
        {
            // Actual cost for this industry + prorated share of untagged costs
            var actualCostForBt = costByBt?.GetValueOrDefault(btId) ?? 0m;
            var proratedUntaggedCost = totalRevenue > 0m ? untaggedCost * (revenue / totalRevenue) : 0m;
            var costForBt = actualCostForBt + proratedUntaggedCost;

            var profitPerIndustry = Math.Max(0m, revenue - costForBt);
            var rate = perBtTaxRates.GetValueOrDefault((btId.ToString(), RowDefinitionConstants.TaxType.Pit));
            total += profitPerIndustry * rate;
        }
        return Math.Round(total, 0);
    }

    /// <summary>
    /// Same as ComputePerIndustryPitMethod2 but also returns a per-industry breakdown list.
    /// </summary>
    private static (decimal Total, List<TaxBreakdownDetail> Breakdown) ComputePerIndustryPitMethod2WithBreakdown(
        decimal totalRevenue,
        decimal totalCost,
        IReadOnlyDictionary<Guid, decimal>? revenueByBt,
        IReadOnlyDictionary<(string, string), decimal> perBtTaxRates,
        IReadOnlyDictionary<Guid, decimal>? costByBt = null)
    {
        var breakdown = new List<TaxBreakdownDetail>();
        if (revenueByBt == null || totalRevenue <= 0m)
            return (0m, breakdown);

        var taggedCost = costByBt?.Values.Sum() ?? 0m;
        var untaggedCost = Math.Max(0m, totalCost - taggedCost);

        decimal total = 0m;
        foreach (var (btId, revenue) in revenueByBt)
        {
            var actualCostForBt = costByBt?.GetValueOrDefault(btId) ?? 0m;
            var proratedUntaggedCost = totalRevenue > 0m ? untaggedCost * (revenue / totalRevenue) : 0m;
            var costForBt = actualCostForBt + proratedUntaggedCost;

            var profitPerIndustry = Math.Max(0m, revenue - costForBt);
            var rate = perBtTaxRates.GetValueOrDefault((btId.ToString(), RowDefinitionConstants.TaxType.Pit));
            var taxAmount = profitPerIndustry * rate;
            total += taxAmount;

            breakdown.Add(new TaxBreakdownDetail
            {
                BusinessTypeId = btId,
                Revenue = revenue,
                Cost = Math.Round(costForBt, 0),
                Profit = profitPerIndustry,
                TaxRate = rate,
                TaxAmount = Math.Round(taxAmount, 0),
                Explanation = $"{revenue:#,0} - {costForBt:#,0} = {profitPerIndustry:#,0} x {rate:P4} = {Math.Round(taxAmount, 0):#,0}"
            });
        }
        return (Math.Round(total, 0), breakdown);
    }

    private record TaxBreakdownDetail
    {
        public Guid BusinessTypeId { get; init; }
        public decimal Revenue { get; init; }
        public decimal Cost { get; init; }
        public decimal Profit { get; init; }
        public decimal TaxRate { get; init; }
        public decimal TaxAmount { get; init; }
        public string? Explanation { get; init; }
    }

    /// <summary>
    /// Compute PIT per-industry for Cách 1 (S2a):
    ///   Σ revenue_i × PIT_rate_i, but ONLY if totalRevenue > 500M threshold.
    /// </summary>
    private static decimal ComputePerIndustryPitMethod1(
        decimal totalRevenue,
        IReadOnlyDictionary<Guid, decimal>? revenueByBt,
        IReadOnlyDictionary<(string, string), decimal> perBtTaxRates)
    {
        const decimal PIT_THRESHOLD = 500_000_000m;
        if (revenueByBt == null || totalRevenue <= PIT_THRESHOLD) return 0m;

        decimal total = 0m;
        foreach (var (btId, revenue) in revenueByBt)
        {
            var rate = perBtTaxRates.GetValueOrDefault((btId.ToString(), RowDefinitionConstants.TaxType.Pit));
            total += revenue * rate;
        }
        return Math.Round(total, 0);
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
                ?? row.Values.GetValueOrDefault("CostId")?.ToString(),

            // Revenue/Order
            "revenue" or "so_tien" => row.Values.GetValueOrDefault("Amount"),

            // GL Entries (S2e)
            "thu_vao" => row.Values.GetValueOrDefault("DebitAmount"),
            "chi_ra" => row.Values.GetValueOrDefault("CreditAmount"),

            // Section (S2c, S2e)
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

    private async Task<Dictionary<Guid, decimal>> LoadCostByBusinessTypeAsync(BookRenderContext context)
    {
        var query = new Application.DTOs.Cost.CostQueryParams
        {
            BusinessLocationId = context.BusinessLocationId,
            FromDate = context.PeriodStart,
            ToDate = context.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (items, _) = await _uow.Costs.SearchAsync(query);
        return items
            .Where(c => c.DeletedAt == null && c.BusinessTypeId.HasValue)
            .GroupBy(c => c.BusinessTypeId!.Value)
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

        // Future: "ProductId" → LoadAmountByProductAsync, etc.
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
