using System.Text.Json;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using BizFlow.Infrastructure.Services.FormulaEngine;
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

            // Attach money channel for GL entry templates (S2e) so client can distinguish cash vs bank.
            if (sourceRow.Values.ContainsKey("MoneyChannel"))
                row["moneyChannel"] = sourceRow.Values.GetValueOrDefault("MoneyChannel")?.ToString();

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

        var (formulaValues, formulaIdToCode, formulaBreakdowns) = await EvaluateTemplateFormulasAsync(context, version, rowDefinitions);
        var (formulaIdToValue, formulaIdToBreakdown) = BuildFormulaLookups(rowDefinitions, formulaValues, formulaIdToCode, formulaBreakdowns);

        var taxRates = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(context.RulesetId, context.BusinessTypeIds);
        var taxRateLookup = taxRates
            .GroupBy(x => NormalizeTaxType(x.TaxType))
            .ToDictionary(g => g.Key, g => g.First().TaxRate, StringComparer.OrdinalIgnoreCase);

        var startOfBookDefs = rowDefinitions
            .Where(r => r.Position == RowDefinitionConstants.Position.StartOfBook)
            .OrderBy(r => r.SortOrder)
            .ToList();

        var perGroupDefs = rowDefinitions
            .Where(r => r.Position == RowDefinitionConstants.Position.PerGroup)
            .OrderBy(r => r.SortOrder)
            .ToList();

        var perSectionDefs = rowDefinitions
            .Where(r => r.Position == RowDefinitionConstants.Position.PerSection)
            .OrderBy(r => r.SortOrder)
            .ToList();

        var footerDefs = rowDefinitions
            .Where(r => r.Position == RowDefinitionConstants.Position.EndOfBook)
            .OrderBy(r => r.SortOrder)
            .ToList();

        var sections = new List<BookSectionDto>();
        var groupedTaxTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var groupIndex = 0;

        if (perGroupDefs.Count > 0)
        {
            var (groupSections, groupTaxTotals, nextIndex) = await BuildPerGroupSectionsAsync(
                context, perGroupDefs, startOfBookDefs, formulaIdToValue, formulaIdToBreakdown, taxRates, groupIndex);
            sections.AddRange(groupSections);
            MergeTotals(groupedTaxTotals, groupTaxTotals);
            groupIndex = nextIndex;
        }

        if (perSectionDefs.Count > 0)
        {
            var (sectionSections, sectionTaxTotals, nextIndex) = await BuildPerSectionSectionsAsync(
                context, perSectionDefs, formulaIdToValue, taxRateLookup, formulaValues, groupIndex);
            sections.AddRange(sectionSections);
            MergeTotals(groupedTaxTotals, sectionTaxTotals);
            groupIndex = nextIndex;
        }

        var footerRows = await BuildFooterRowsAsync(
            context, footerDefs, formulaIdToValue, formulaIdToBreakdown, taxRates, taxRateLookup, formulaValues, groupedTaxTotals);

        return new BookSectionsRenderResult
        {
            Columns = columns,
            Sections = sections,
            FooterRows = footerRows
        };
    }

    // ────────────────────────────────────────────────────────
    // RENDER SECTIONS — Path A: per_group (data-driven)
    // ────────────────────────────────────────────────────────
    private async Task<(List<BookSectionDto> Sections, Dictionary<string, decimal> TaxTotals, int NextGroupIndex)>
        BuildPerGroupSectionsAsync(
            BookRenderContext context,
            List<TemplateRowDefinition> perGroupDefs,
            List<TemplateRowDefinition> startOfBookDefs,
            IReadOnlyDictionary<long, decimal> formulaIdToValue,
            IReadOnlyDictionary<long, Dictionary<string, decimal>> formulaIdToBreakdown,
            List<IndustryTaxRate> taxRates,
            int startGroupIndex)
    {
        var sections = new List<BookSectionDto>();
        var taxTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var groupIndex = startGroupIndex;

        var groupByField = perGroupDefs.Select(d => d.GroupByField).FirstOrDefault(f => !string.IsNullOrWhiteSpace(f));
        var sectionType = perGroupDefs.Select(d => d.SectionType).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "group";

        var (groupKeys, groupNames, groupAmounts, groupTaxRates) = await ResolveGroupDataAsync(context, groupByField);

        Dictionary<string, (decimal Qty, decimal Value)>? openingBalances = null;
        if (startOfBookDefs.Count > 0 && "ProductId".Equals(groupByField, StringComparison.OrdinalIgnoreCase))
            openingBalances = await ComputeProductOpeningBalancesAsync(context);

        foreach (var groupKey in groupKeys)
        {
            groupIndex++;
            var subtotal = groupAmounts.GetValueOrDefault(groupKey);
            var groupName = groupNames.GetValueOrDefault(groupKey) ?? groupKey;

            var sectionRows = new List<Dictionary<string, object?>>();

            if (openingBalances != null)
            {
                var balance = openingBalances.GetValueOrDefault(groupKey);
                foreach (var startDef in startOfBookDefs)
                {
                    var openingRow = new Dictionary<string, object?> { ["lineType"] = startDef.RowType };
                    var label = ResolveRowLabel(startDef.RowLabel, groupIndex, groupName);
                    if (!string.IsNullOrWhiteSpace(label))
                        openingRow["dien_giai"] = label;
                    openingRow["sl_ton"] = balance.Qty;
                    openingRow["tien_ton"] = balance.Value;
                    sectionRows.Add(openingRow);
                }
            }

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

                if (rowDef.RowType == RowDefinitionConstants.RowType.Subtotal
                    || rowDef.RowType == RowDefinitionConstants.RowType.SectionSubtotal)
                {
                    var subtotalValue = ResolveFormulaValue(rowDef, formulaIdToValue) ?? subtotal;
                    row[GetValueFieldCode(rowDef)] = subtotalValue;
                    row["explanation"] = $"Tổng doanh thu nhóm \"{groupName}\": {FormatNumber(subtotalValue)} đ";
                }
                else if (rowDef.RowType == RowDefinitionConstants.RowType.TaxLine)
                {
                    var taxType = NormalizeTaxType(rowDef.TaxType);
                    decimal taxAmount;

                    // Use formula breakdown for per-group tax; fall back to total formula value if no breakdown
                    if (rowDef.FormulaId.HasValue
                        && formulaIdToBreakdown.TryGetValue(rowDef.FormulaId.Value, out var breakdown))
                        breakdown.TryGetValue(groupKey, out taxAmount);
                    else
                        taxAmount = ResolveFormulaValue(rowDef, formulaIdToValue) ?? 0m;

                    var rateSource = GetTaxRateSource(rowDef.Formula);
                    var taxRate = ResolveTaxRate(
                        rateSource,
                        groupKey,
                        taxType,
                        groupTaxRates,
                        taxRates,
                        null);

                    row[GetValueFieldCode(rowDef)] = taxAmount;
                    row["taxMetadata"] = new Dictionary<string, object?>
                    {
                        ["taxType"] = rowDef.TaxType,
                        ["rate"] = taxRate,
                        ["source"] = "FORMULA"
                    };

                    row["explanation"] = BuildPerGroupTaxExplanation(
                        rowDef.Formula,
                        subtotal,
                        taxAmount,
                        taxRate);

                    if (!string.IsNullOrWhiteSpace(taxType))
                        taxTotals[taxType] = taxTotals.GetValueOrDefault(taxType) + taxAmount;
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

        return (sections, taxTotals, groupIndex);
    }

    // ────────────────────────────────────────────────────────
    // RENDER SECTIONS — Path B: per_section (sequential)
    // ────────────────────────────────────────────────────────
    private async Task<(List<BookSectionDto> Sections, Dictionary<string, decimal> TaxTotals, int NextGroupIndex)>
        BuildPerSectionSectionsAsync(
            BookRenderContext context,
            List<TemplateRowDefinition> perSectionDefs,
            IReadOnlyDictionary<long, decimal> formulaIdToValue,
            IReadOnlyDictionary<string, decimal> taxRateLookup,
            IReadOnlyDictionary<string, decimal> formulaValues,
            int startGroupIndex)
    {
        var sections = new List<BookSectionDto>();
        var taxTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var groupIndex = startGroupIndex;
        var logicalSections = SplitDefinitionsByHeader(perSectionDefs);

        // Lazy-loaded cache — at most one DB call per data type across the entire path
        Dictionary<Guid, decimal>? revenueByBtCache = null;
        Dictionary<Guid, string>? btNamesCache = null;
        Dictionary<string, decimal>? costByTypeCache = null;

        foreach (var (headerDef, bodyDefs) in logicalSections)
        {
            groupIndex++;
            var sectionType = headerDef?.SectionType ?? perSectionDefs.First().SectionType ?? "section";
            var sectionName = headerDef?.RowLabel ?? $"Section {groupIndex}";
            var sectionFilterValue = headerDef?.SectionFilterValue ?? sectionName;

            var sectionRows = new List<Dictionary<string, object?>>();

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

                if (rowDef.RowType == RowDefinitionConstants.RowType.CostTypeSubtotal)
                {
                    costByTypeCache ??= await LoadCostByCostTypeAsync(context);
                    var costTypes = ParseCostTypeFilter(rowDef.SectionFilterValue);
                    var amount = costTypes.Sum(ct => costByTypeCache.GetValueOrDefault(ct, 0m));
                    if (!string.IsNullOrWhiteSpace(rowDef.RowLabel))
                        row["dien_giai"] = rowDef.RowLabel;
                    row[GetValueFieldCode(rowDef)] = amount;
                    row["explanation"] = $"Tổng: {FormatNumber(amount)} đ";
                    sectionRows.Add(row);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(rowDef.RowLabel))
                    row["dien_giai"] = rowDef.RowLabel;

                var valueField = GetValueFieldCode(rowDef);
                var formulaVal = ResolveFormulaValue(rowDef, formulaIdToValue);
                if (formulaVal.HasValue)
                    row[valueField] = formulaVal.Value;

                // For subtotal rows in revenue/cost sections, build a concise Vietnamese explanation
                if (rowDef.RowType == RowDefinitionConstants.RowType.SectionSubtotal
                    && sectionFilterValue.Equals("revenue", StringComparison.OrdinalIgnoreCase))
                {
                    revenueByBtCache ??= await LoadRevenueByBusinessTypeAsync(context);
                    if (revenueByBtCache.Count > 0)
                    {
                        btNamesCache ??= await LoadBusinessTypeNamesAsync(revenueByBtCache.Keys);
                        var breakdownText = string.Join("; ", revenueByBtCache
                            .OrderByDescending(kv => kv.Value)
                            .Select(kv => $"{btNamesCache.GetValueOrDefault(kv.Key) ?? kv.Key.ToString()}: {FormatNumber(kv.Value)} đ"));
                        row["explanation"] = $"Gồm: {breakdownText}";
                    }
                }
                else if (rowDef.RowType == RowDefinitionConstants.RowType.SectionSubtotal
                    && sectionFilterValue.Equals("cost", StringComparison.OrdinalIgnoreCase))
                {
                    row["explanation"] = $"Tổng chi phí hợp lý: {FormatNumber(formulaVal ?? 0m)} đ";
                }

                if (rowDef.RowType == RowDefinitionConstants.RowType.TaxLine)
                {
                    var taxType = NormalizeTaxType(rowDef.TaxType);
                    var taxAmount = ResolveFormulaValue(rowDef, formulaIdToValue) ?? formulaVal ?? 0m;

                    row[valueField] = taxAmount;
                    var formulaExplanation = BuildFormulaExplanation(rowDef.Formula, formulaValues, taxAmount);
                    row["explanation"] = formulaExplanation ?? $"Thuế: {FormatNumber(taxAmount)} đ";
                    var rateSource = GetTaxRateSource(rowDef.Formula);
                    var taxRate = ResolveTaxRate(
                        rateSource,
                        null,
                        taxType,
                        null,
                        Array.Empty<IndustryTaxRate>(),
                        taxRateLookup);

                    row["taxMetadata"] = new Dictionary<string, object?>
                    {
                        ["taxType"] = rowDef.TaxType,
                        ["rate"] = taxRate,
                        ["source"] = "FORMULA"
                    };
                    if (!string.IsNullOrWhiteSpace(taxType))
                        taxTotals[taxType] = taxTotals.GetValueOrDefault(taxType) + taxAmount;
                }

                if (rowDef.Formula != null && !row.ContainsKey("explanation"))
                {
                    var explanation = BuildFormulaExplanation(rowDef.Formula, formulaValues, formulaVal);
                    if (!string.IsNullOrWhiteSpace(explanation))
                        row["explanation"] = explanation;
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

        return (sections, taxTotals, groupIndex);
    }

    // ────────────────────────────────────────────────────────
    // RENDER FOOTER — end_of_book rows
    // ────────────────────────────────────────────────────────
    private async Task<List<Dictionary<string, object?>>> BuildFooterRowsAsync(
        BookRenderContext context,
        List<TemplateRowDefinition> footerDefs,
        IReadOnlyDictionary<long, decimal> formulaIdToValue,
        IReadOnlyDictionary<long, Dictionary<string, decimal>> formulaIdToBreakdown,
        List<IndustryTaxRate> taxRates,
        IReadOnlyDictionary<string, decimal> taxRateLookup,
        IReadOnlyDictionary<string, decimal> formulaValues,
        Dictionary<string, decimal> groupedTaxTotals)
    {
        var footerRows = new List<Dictionary<string, object?>>();

        // Lazy-loaded cache — at most one DB call per data type across the entire footer loop
        Dictionary<Guid, string>? btNamesCache = null;
        Dictionary<Guid, decimal>? revenueByBtCache = null;
        Dictionary<Guid, decimal>? costByBtCache = null;
        decimal? revenueTotalCache = null;
        decimal? costTotalCache = null;

        foreach (var rowDef in footerDefs)
        {
            var row = new Dictionary<string, object?> { ["lineType"] = rowDef.RowType };

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
                var isS2c = context.TemplateCode.Equals("S2c", StringComparison.OrdinalIgnoreCase);

                row[footerValueField] = taxAmount;
                var rateSource = GetTaxRateSource(rowDef.Formula);
                var taxRate = ResolveTaxRate(
                    rateSource,
                    null,
                    taxType,
                    null,
                    taxRates,
                    taxRateLookup);

                row["taxMetadata"] = new Dictionary<string, object?>
                {
                    ["taxType"] = rowDef.TaxType,
                    ["rate"] = taxRate,
                    ["source"] = "FORMULA"
                };

                if (isS2c && taxRate.HasValue)
                {
                    revenueTotalCache ??= await LoadRevenueTotalAsync(context);
                    costTotalCache ??= await LoadCostTotalAsync(context);
                    var grossProfit = revenueTotalCache.Value - costTotalCache.Value;

                    row["explanation"] =
                        $"(Doanh thu {FormatNumber(revenueTotalCache.Value)} - Chi phí {FormatNumber(costTotalCache.Value)} = Lợi nhuận {FormatNumber(grossProfit)}) x {taxRate.Value * 100:0.##}% = {FormatNumber(taxAmount)} đ";
                }
                else
                {
                    var formulaExplanation = BuildFormulaExplanation(rowDef.Formula, formulaValues, taxAmount);
                    row["explanation"] = formulaExplanation ?? $"Thuế: {FormatNumber(taxAmount)} đ";
                }

                // Build taxBreakdown from formula breakdown if available
                if (!isS2c
                    && rowDef.FormulaId.HasValue
                    && formulaIdToBreakdown.TryGetValue(rowDef.FormulaId.Value, out var breakdown)
                    && breakdown.Count > 0)
                {
                    revenueByBtCache ??= await LoadRevenueByBusinessTypeAsync(context);
                    costByBtCache ??= await LoadCostByBusinessTypeAsync(context);
                    if (btNamesCache == null)
                    {
                        var allBtIds = revenueByBtCache.Keys.Concat(costByBtCache.Keys).Distinct();
                        btNamesCache = await LoadBusinessTypeNamesAsync(allBtIds);
                    }

                    Dictionary<(string, string), decimal>? breakdownTaxRates = null;
                    if (rateSource.Type == TaxRateSourceType.Lookup)
                    {
                        var btIds = breakdown.Keys
                            .Select(k => Guid.TryParse(k, out var gid) ? (Guid?)gid : null)
                            .Where(g => g.HasValue)
                            .Select(g => g!.Value)
                            .ToList();

                        var rateList = taxRates;
                        if (rateList.Count == 0 && btIds.Count > 0)
                            rateList = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(context.RulesetId, btIds);

                        breakdownTaxRates = rateList
                            .GroupBy(x => (x.BusinessTypeId.ToString(), NormalizeTaxType(x.TaxType)))
                            .ToDictionary(g => g.Key, g => g.First().TaxRate);
                    }

                    row["taxBreakdown"] = breakdown.Select(kv =>
                    {
                        var groupKey = kv.Key;
                        var taxAmt = kv.Value;
                        var revenue = Guid.TryParse(groupKey, out var btId) ? revenueByBtCache.GetValueOrDefault(btId) : 0m;
                        var cost = Guid.TryParse(groupKey, out var btId2) ? costByBtCache.GetValueOrDefault(btId2) : 0m;
                        var profit = Math.Max(0m, revenue - cost);

                        var rate = ResolveTaxRate(
                            rateSource,
                            groupKey,
                            taxType,
                            breakdownTaxRates,
                            taxRates,
                            taxRateLookup);

                        string? itemExplanation = null;
                        if (rate.HasValue)
                        {
                            var baseLabel = cost != 0m
                                ? $"(Doanh thu {revenue:#,0} - Chi phí {cost:#,0} = Lợi nhuận {profit:#,0})"
                                : $"Doanh thu {revenue:#,0}";
                            itemExplanation = $"{baseLabel} x {rate.Value * 100:0.##}% = {taxAmt:#,0} đ";
                        }

                        return new Dictionary<string, object?>
                        {
                            ["businessTypeId"] = groupKey,
                            ["businessTypeName"] = btNamesCache.GetValueOrDefault(Guid.TryParse(groupKey, out var ng) ? ng : Guid.Empty) ?? groupKey,
                            ["revenue"] = revenue,
                            ["cost"] = cost,
                            ["profit"] = profit,
                            ["taxRate"] = rate ?? 0m,
                            ["taxAmount"] = taxAmt,
                            ["explanation"] = itemExplanation
                        };
                    }).ToList();

                    var taxBreakdownList = (List<Dictionary<string, object?>>)row["taxBreakdown"]!;
                    if (taxBreakdownList.Count > 0)
                    {
                        var condensed = string.Join("; ", taxBreakdownList
                            .Select(item => $"{item["businessTypeName"]}: {FormatNumber((decimal)(item["taxAmount"] ?? 0m))} đ"));
                        row["explanation"] = $"Gồm: {condensed}";
                    }
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
                row["explanation"] = $"Tổng cộng: {FormatNumber(grandTotalValue)} đ";
            }
            else
            {
                var formulaVal = ResolveFormulaValue(rowDef, formulaIdToValue);
                if (formulaVal.HasValue)
                    row[footerValueField] = formulaVal.Value;

                if (rowDef.Formula != null && !row.ContainsKey("explanation"))
                {
                    var explanation = BuildFormulaExplanation(rowDef.Formula, formulaValues, formulaVal);
                    if (!string.IsNullOrWhiteSpace(explanation))
                        row["explanation"] = explanation;
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

        return footerRows;
    }

    // ────────────────────────────────────────────────────────
    // FORMULA LOOKUP HELPERS
    // ────────────────────────────────────────────────────────
    private static (Dictionary<long, decimal> IdToValue, Dictionary<long, Dictionary<string, decimal>> IdToBreakdown)
        BuildFormulaLookups(
            IEnumerable<TemplateRowDefinition> rowDefinitions,
            IReadOnlyDictionary<string, decimal> formulaValues,
            IReadOnlyDictionary<long, string> formulaIdToCode,
            IReadOnlyDictionary<string, Dictionary<string, decimal>> formulaBreakdowns)
    {
        var idToValue = new Dictionary<long, decimal>();
        var idToBreakdown = new Dictionary<long, Dictionary<string, decimal>>();
        foreach (var rd in rowDefinitions.Where(r => r.FormulaId.HasValue))
        {
            // Prefer the code from the evaluation result (draft-aware) over the stale navigation property
            var code = formulaIdToCode.TryGetValue(rd.FormulaId!.Value, out var evaluatedCode)
                ? evaluatedCode
                : rd.Formula?.Code;

            if (code == null) continue;

            if (formulaValues.TryGetValue(code, out var val))
                idToValue[rd.FormulaId!.Value] = val;
            if (formulaBreakdowns.TryGetValue(code, out var bd))
                idToBreakdown[rd.FormulaId!.Value] = bd;
        }
        return (idToValue, idToBreakdown);
    }

    private static void MergeTotals(Dictionary<string, decimal> target, IReadOnlyDictionary<string, decimal> source)
    {
        foreach (var kv in source)
            target[kv.Key] = target.GetValueOrDefault(kv.Key) + kv.Value;
    }

    private static string? BuildFormulaExplanation(
        FormulaDefinition? formula,
        IReadOnlyDictionary<string, decimal> formulaValues,
        decimal? resultValue)
    {
        if (formula == null || resultValue == null || string.IsNullOrWhiteSpace(formula.ExpressionJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(formula.ExpressionJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(AstNode.Foreach, out _))
                return null;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(AstNode.Apply, out var apply))
                root = apply;

            if (!TryExplainNode(root, formulaValues, out var text, out _))
                return null;

            return $"{text} = {FormatNumber(resultValue.Value)}";
        }
        catch
        {
            return null;
        }
    }

    private static bool TryExplainNode(
        JsonElement node,
        IReadOnlyDictionary<string, decimal> formulaValues,
        out string text,
        out decimal? value)
    {
        text = string.Empty;
        value = null;

        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("literal", out var literal))
            {
                var val = literal.GetDecimal();
                text = FormatNumber(val);
                value = val;
                return true;
            }

            if (node.TryGetProperty(AstNode.Ref, out var refNode))
            {
                var refCode = refNode.GetString();
                if (!string.IsNullOrWhiteSpace(refCode)
                    && formulaValues.TryGetValue(refCode, out var refVal))
                {
                    text = FormatNumber(refVal);
                    value = refVal;
                }
                else
                {
                    text = refCode ?? "?";
                }
                return true;
            }

            if (node.TryGetProperty(AstNode.Aggregate, out var aggNode))
            {
                var aggType = aggNode.GetString() ?? "SUM";
                var source = node.TryGetProperty(AstNode.Source, out var src) ? src.GetString() : "?";
                var field = node.TryGetProperty(AstNode.Field, out var fld) ? fld.GetString() : "?";
                text = $"{aggType}({source}.{field})";
                return true;
            }

            if (node.TryGetProperty(AstNode.Lookup, out var lookupNode))
            {
                var entity = lookupNode.TryGetProperty(AstNode.Entity, out var ent) ? ent.GetString() : "?";
                var field = lookupNode.TryGetProperty(AstNode.Field, out var fld) ? fld.GetString() : "?";
                text = $"Lookup({entity}.{field})";
                return true;
            }

            if (node.TryGetProperty(AstNode.Context, out var contextNode))
            {
                var contextKey = contextNode.GetString();
                if (!string.IsNullOrWhiteSpace(contextKey)
                    && (formulaValues.TryGetValue(contextKey, out var contextVal)
                        || formulaValues.TryGetValue(contextKey.ToUpperInvariant(), out contextVal)
                        || formulaValues.TryGetValue(contextKey.ToLowerInvariant(), out contextVal)))
                {
                    text = FormatNumber(contextVal);
                    value = contextVal;
                }
                else
                {
                    text = $"[{contextKey ?? "context"}]";
                }

                return true;
            }

            if (node.TryGetProperty(AstNode.Apply, out var applyNode))
            {
                return TryExplainNode(applyNode, formulaValues, out text, out value);
            }

            if (node.TryGetProperty(AstNode.Op, out var opNode))
            {
                var op = opNode.GetString() ?? "";
                TryExplainNode(node.GetProperty(AstNode.Left), formulaValues, out var leftText, out var leftVal);
                TryExplainNode(node.GetProperty(AstNode.Right), formulaValues, out var rightText, out var rightVal);

                var symbol = op.ToUpperInvariant() switch
                {
                    OpType.Add => "+",
                    OpType.Subtract => "-",
                    OpType.Multiply => "x",
                    OpType.Divide => "/",
                    _ => op
                };

                text = $"{leftText} {symbol} {rightText}";

                if (leftVal.HasValue && rightVal.HasValue)
                    value = ComputeOp(op, leftVal.Value, rightVal.Value);

                return true;
            }

            if (node.TryGetProperty(AstNode.Fn, out var fnNode))
            {
                var fn = fnNode.GetString() ?? "";
                var argTexts = new List<string>();
                var argValues = new List<decimal>();

                if (node.TryGetProperty(AstNode.Args, out var argsNode))
                {
                    foreach (var arg in argsNode.EnumerateArray())
                    {
                        TryExplainNode(arg, formulaValues, out var argText, out var argVal);
                        argTexts.Add(argText);
                        if (argVal.HasValue)
                            argValues.Add(argVal.Value);
                    }
                }

                text = $"{fn.ToUpperInvariant()}({string.Join(", ", argTexts)})";
                if (argValues.Count == argTexts.Count && argValues.Count > 0)
                    value = ComputeFn(fn, argValues);

                return true;
            }

            if (node.TryGetProperty(AstNode.Foreach, out _))
                return false;
        }

        return false;
    }

    private static decimal ComputeOp(string op, decimal left, decimal right) =>
        op.ToUpperInvariant() switch
        {
            OpType.Add => left + right,
            OpType.Subtract => left - right,
            OpType.Multiply => left * right,
            OpType.Divide => right != 0 ? left / right : 0m,
            _ => 0m
        };

    private static decimal ComputeFn(string fn, IReadOnlyList<decimal> args) =>
        fn.ToUpperInvariant() switch
        {
            FnType.Max => args.Count > 0 ? args.Max() : 0m,
            FnType.Min => args.Count > 0 ? args.Min() : 0m,
            FnType.Abs => args.Count > 0 ? Math.Abs(args[0]) : 0m,
            _ => 0m
        };

    private static string FormatNumber(decimal value) => value.ToString("#,0.##");

    private static string BuildPerGroupTaxExplanation(
        FormulaDefinition? formula,
        decimal subtotal,
        decimal taxAmount,
        decimal? taxRate)
    {
        if (!taxRate.HasValue)
            return $"Thuế: {FormatNumber(taxAmount)} đ";

        var rate = taxRate.Value;
        if (rate <= 0m)
            return $"Thuế: {FormatNumber(taxAmount)} đ";

        var deductionAmount = TryGetForeachDeductionAmount(formula);
        if (!deductionAmount.HasValue)
            return $"{subtotal:#,0} x {rate * 100:0.##}% = {taxAmount:#,0.##} đ";

        var taxableBase = Math.Round(taxAmount / rate, 2, MidpointRounding.AwayFromZero);
        var deductionApplied = Math.Max(0m, subtotal - taxableBase);

        if (deductionApplied <= 0m)
            return $"{subtotal:#,0} x {rate * 100:0.##}% = {taxAmount:#,0.##} đ (không áp dụng giảm trừ)";

        return
            $"MAX(0, {subtotal:#,0} - {deductionAmount.Value:#,0}) = {taxableBase:#,0.##}; " +
            $"giảm trừ áp dụng: {deductionApplied:#,0.##} đ (tối đa {deductionAmount.Value:#,0} đ); " +
            $"{taxableBase:#,0.##} x {rate * 100:0.##}% = {taxAmount:#,0.##} đ";
    }

    private static decimal? TryGetForeachDeductionAmount(FormulaDefinition? formula)
    {
        if (string.IsNullOrWhiteSpace(formula?.ExpressionJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(formula.ExpressionJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty(AstNode.Foreach, out _)
                || !root.TryGetProperty("deduction", out var deductionNode)
                || deductionNode.ValueKind != JsonValueKind.Object
                || !deductionNode.TryGetProperty("amount", out var amountNode))
                return null;

            return amountNode.GetDecimal();
        }
        catch
        {
            return null;
        }
    }

    private enum TaxRateSourceType
    {
        None,
        Literal,
        Lookup
    }

    private readonly record struct TaxRateSource(TaxRateSourceType Type, decimal? Literal, string? TaxType);

    private static TaxRateSource GetTaxRateSource(FormulaDefinition? formula)
    {
        if (string.IsNullOrWhiteSpace(formula?.ExpressionJson))
            return new TaxRateSource(TaxRateSourceType.None, null, null);

        try
        {
            using var doc = JsonDocument.Parse(formula.ExpressionJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("foreach", out _) && root.TryGetProperty("apply", out var apply))
                root = apply;

            return TryFindRateSource(root, out var source)
                ? source
                : new TaxRateSource(TaxRateSourceType.None, null, null);
        }
        catch
        {
            return new TaxRateSource(TaxRateSourceType.None, null, null);
        }
    }

    private static bool TryFindRateSource(JsonElement node, out TaxRateSource source)
    {
        source = new TaxRateSource(TaxRateSourceType.None, null, null);

        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("op", out var opNode)
                && string.Equals(opNode.GetString(), "MULTIPLY", StringComparison.OrdinalIgnoreCase))
            {
                var left = node.GetProperty("left");
                var right = node.GetProperty("right");

                var leftIsRate = TryReadRateNode(left, out var leftRate);
                var rightIsRate = TryReadRateNode(right, out var rightRate);

                if (leftIsRate && !rightIsRate)
                {
                    source = leftRate;
                    return true;
                }

                if (rightIsRate && !leftIsRate)
                {
                    source = rightRate;
                    return true;
                }

                if (leftIsRate)
                {
                    source = leftRate;
                    return true;
                }

                if (rightIsRate)
                {
                    source = rightRate;
                    return true;
                }
            }

            foreach (var prop in node.EnumerateObject())
            {
                if (TryFindRateSource(prop.Value, out source))
                    return true;
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                if (TryFindRateSource(item, out source))
                    return true;
            }
        }

        return false;
    }

    private static bool TryReadRateNode(JsonElement node, out TaxRateSource source)
    {
        source = new TaxRateSource(TaxRateSourceType.None, null, null);

        if (node.ValueKind != JsonValueKind.Object)
            return false;

        if (node.TryGetProperty("literal", out var literal))
        {
            var value = literal.GetDecimal();
            source = new TaxRateSource(TaxRateSourceType.Literal, value, null);
            return true;
        }

        if (node.TryGetProperty("lookup", out var lookup))
        {
            string? taxType = null;
            if (lookup.TryGetProperty("filter", out var filter)
                && filter.TryGetProperty("TaxType", out var taxTypeNode))
            {
                taxType = taxTypeNode.GetString();
            }

            source = new TaxRateSource(TaxRateSourceType.Lookup, null, taxType);
            return true;
        }

        return false;
    }

    private static decimal? ResolveTaxRate(
        TaxRateSource rateSource,
        string? groupKey,
        string? defaultTaxType,
        IReadOnlyDictionary<(string, string), decimal>? groupTaxRates,
        IReadOnlyList<IndustryTaxRate> taxRates,
        IReadOnlyDictionary<string, decimal>? taxRateLookup)
    {
        if (rateSource.Type == TaxRateSourceType.Literal)
            return rateSource.Literal;

        if (rateSource.Type != TaxRateSourceType.Lookup)
            return null;

        var taxType = NormalizeTaxType(rateSource.TaxType ?? defaultTaxType);
        if (string.IsNullOrWhiteSpace(taxType))
            return null;

        if (!string.IsNullOrWhiteSpace(groupKey))
        {
            if (groupTaxRates != null
                && groupTaxRates.TryGetValue((groupKey, taxType), out var groupRate))
                return groupRate;

            if (Guid.TryParse(groupKey, out var gid))
            {
                var rateEntry = taxRates.FirstOrDefault(r =>
                    r.BusinessTypeId == gid
                    && string.Equals(NormalizeTaxType(r.TaxType), taxType, StringComparison.OrdinalIgnoreCase));
                if (rateEntry != null)
                    return rateEntry.TaxRate;
            }

            return null;
        }

        if (taxRateLookup != null && taxRateLookup.TryGetValue(taxType, out var lookupRate))
            return lookupRate;

        return null;
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
        EvaluateTemplateFormulasAsync(
            BookRenderContext context,
            AccountingTemplateVersion version,
            IEnumerable<TemplateRowDefinition>? rowDefinitions = null)
    {
        var formulaIds = version.FieldMappings
            .Where(m => m.FormulaId != null)
            .Select(m => m.FormulaId!.Value)
            // Also collect FormulaIds from RowDefinitions (tax_line, balance_row etc.)
            // so draft-mapped formulas on those rows are evaluated via GetByIdsAsync.
            .Concat(rowDefinitions?
                .Where(r => r.FormulaId.HasValue)
                .Select(r => r.FormulaId!.Value) ?? Enumerable.Empty<long>())
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
        var cursorOffset = ParseCursorOffset(cursor);
        var pageNumber = cursorOffset > 0 ? (cursorOffset / safeBatchSize) + 1 : 1;

        var query = new Application.DTOs.Revenue.RevenueQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = pageNumber,
            PageSize = safeBatchSize + 1, // +1 to check hasMore
            IncludeReversal = true,
            ExcludeCancelled = true   // push filter to DB so hasMore/cursor is accurate
        };

        var (items, totalCount) = await _uow.Revenues.SearchAsync(query);
        var list = items.ToList();

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
                    ["Status"] = r.Status,
                    ["IsReversal"] = r.IsReversal,
                    ["ReversedRevenueId"] = r.ReversedRevenueId,
                    ["RefRevenueId"] = r.RefRevenueId,
                    ["RevenueType"] = r.RevenueType,
                    ["MoneyChannel"] = r.MoneyChannel,
                    ["OrderId"] = r.OrderId,
                    ["DocumentNumber"] = r.DocumentNumber,
                    ["DocumentDate"] = r.DocumentDate,
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
            PageSize = int.MaxValue,
            IncludeReversal = true
        };
        var (costItems, _) = await _uow.Costs.SearchAsync(costQuery);

        var costRows = costItems.Where(c => c.Status != CostStatus.Cancelled).Select(c => new SourceRow
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
                ["Status"] = c.Status,
                ["IsReversal"] = c.IsReversal,
                ["ReversedCostId"] = c.ReversedCostId,
                ["RefCostId"] = c.RefCostId,
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
        var cursorOffset = ParseCursorOffset(cursor);
        var pageNumber = cursorOffset > 0 ? (cursorOffset / safeBatchSize) + 1 : 1;

        var query = new Application.DTOs.GeneralLedger.GeneralLedgerQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = pageNumber,
            PageSize = safeBatchSize + 1,
            ExcludeReversal = true   // push filter to DB so hasMore/cursor is accurate
        };

        var (items, totalCount) = await _uow.GeneralLedgerEntries.SearchAsync(query);
        var list = items.ToList();

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
        var importCostLookup = await _uow.Imports.GetImportCostLookupByLocationAsync(ctx.BusinessLocationId);

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
            Items = list.Select(sm =>
            {
                var costPrice = GetMovementCostPrice(sm, importCostLookup);
                return new SourceRow
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
                        ["CostPrice"] = costPrice,
                        ["Quantity"] = sm.Quantity,
                        ["QuantityAbs"] = Math.Abs(sm.Quantity),
                        ["ImportQty"] = sm.Quantity > 0 ? sm.Quantity : (int?)null,
                        ["ImportValue"] = sm.Quantity > 0 ? Math.Abs(sm.Quantity) * costPrice : (decimal?)null,
                        ["ExportQty"] = sm.Quantity < 0 ? Math.Abs(sm.Quantity) : (int?)null,
                        ["ExportValue"] = sm.Quantity < 0 ? Math.Abs(sm.Quantity) * costPrice : (decimal?)null,
                        ["BalanceAfter"] = sm.BalanceAfter,
                        ["BalanceValue"] = sm.BalanceAfter * costPrice,
                        ["ReferenceType"] = sm.ReferenceType,
                        ["ReferenceId"] = sm.ReferenceId
                    }
                };
            }).ToList(),
            HasMore = hasMore,
            TotalEstimated = totalCount
        };
    }

    /// <summary>
    /// Resolves the cost price for a stock movement:
    /// - For IMPORT movements: uses the cost price recorded on the ProductImport line (import-time price).
    /// - For other movements (ORDER, ADJUSTMENT): falls back to the product's current CostPrice.
    /// </summary>
    private static decimal GetMovementCostPrice(
        StockMovement sm,
        Dictionary<(long ImportId, long ProductId), decimal> importCostLookup)
    {
        if (sm.ReferenceType == StockMovementReferenceType.Import
            && sm.ReferenceId.HasValue
            && importCostLookup.TryGetValue((sm.ReferenceId.Value, sm.ProductId), out var importCostPrice))
            return importCostPrice;
        return sm.Product?.CostPrice ?? 0m;
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
            "so_hieu" => row.Values.GetValueOrDefault("DocumentNumber")?.ToString(),

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
            "OrderId" => row.Values.GetValueOrDefault("OrderId"),
            "OrderCode" => row.Values.GetValueOrDefault("OrderCode"),
            "RevenueType" => row.Values.GetValueOrDefault("RevenueType"),
            "BusinessTypeId" => row.Values.GetValueOrDefault("BusinessTypeId"),
            "DocumentNumber" => row.Values.GetValueOrDefault("DocumentNumber"),
            "DocumentDate" => row.Values.GetValueOrDefault("DocumentDate") ?? row.Date,

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
            PageSize = int.MaxValue,
            IncludeReversal = true
        };

        var (items, _) = await _uow.Revenues.SearchAsync(query);
        return items
            .Where(r => r.Status != RevenueStatus.Cancelled
                && r.BusinessTypeId.HasValue)
            .GroupBy(r => r.BusinessTypeId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
    }

    private async Task<decimal> LoadRevenueTotalAsync(BookRenderContext context)
    {
        var query = new Application.DTOs.Revenue.RevenueQueryParams
        {
            BusinessLocationId = context.BusinessLocationId,
            FromDate = context.PeriodStart,
            ToDate = context.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue,
            IncludeReversal = true
        };

        var (items, _) = await _uow.Revenues.SearchAsync(query);
        return items
            .Where(r => r.Status != RevenueStatus.Cancelled)
            .Sum(r => r.Amount);
    }

    private async Task<Dictionary<Guid, decimal>> LoadCostByBusinessTypeAsync(BookRenderContext context)
    {
        var query = new Application.DTOs.Cost.CostQueryParams
        {
            BusinessLocationId = context.BusinessLocationId,
            FromDate = context.PeriodStart,
            ToDate = context.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue,
            IncludeReversal = true
        };

        var (items, _) = await _uow.Costs.SearchAsync(query);
        return items
            .Where(c => c.Status != CostStatus.Cancelled
                && c.BusinessTypeId.HasValue)
            .GroupBy(c => c.BusinessTypeId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
    }

    private async Task<decimal> LoadCostTotalAsync(BookRenderContext context)
    {
        var query = new Application.DTOs.Cost.CostQueryParams
        {
            BusinessLocationId = context.BusinessLocationId,
            FromDate = context.PeriodStart,
            ToDate = context.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue,
            IncludeReversal = true
        };

        var (items, _) = await _uow.Costs.SearchAsync(query);
        return items
            .Where(c => c.Status != CostStatus.Cancelled)
            .Sum(c => c.Amount);
    }

    private async Task<Dictionary<string, decimal>> LoadCostByCostTypeAsync(BookRenderContext context)
        => await _uow.Costs.SumGroupedByCostTypeAsync(
            context.BusinessLocationId, context.PeriodStart, context.PeriodEnd);

    private static string[] ParseCostTypeFilter(string? sectionFilterValue)
    {
        if (string.IsNullOrWhiteSpace(sectionFilterValue))
            return [];
        return sectionFilterValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
    private async Task<Dictionary<string, (decimal Qty, decimal Value)>> ComputeProductOpeningBalancesAsync(
        BookRenderContext context)
    {
        var allMovements = await _uow.StockMovements.GetByLocationAsync(context.BusinessLocationId);
        var importCostLookup = await _uow.Imports.GetImportCostLookupByLocationAsync(context.BusinessLocationId);

        var result = new Dictionary<string, (decimal Qty, decimal Value)>();
        var preperiodByProduct = allMovements
            .Where(sm => DateOnly.FromDateTime(sm.CreatedAt) < context.PeriodStart)
            .GroupBy(sm => sm.ProductId);

        foreach (var group in preperiodByProduct)
        {
            var last = group
                .OrderByDescending(sm => sm.CreatedAt)
                .ThenByDescending(sm => sm.StockMovementId)
                .First();
            var costPrice = GetMovementCostPrice(last, importCostLookup);
            result[group.Key.ToString()] = (last.BalanceAfter, last.BalanceAfter * costPrice);
        }

        return result;
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
            var amounts = await LoadRevenueByBusinessTypeAsync(context);

            // Use all business types that have any revenue rows in this period
            // (not limited to context.BusinessTypeIds which is derived from products only)
            var activeIds = amounts.Keys.ToList();

            var names = await LoadBusinessTypeNamesAsync(activeIds);

            var taxRates = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(
                context.RulesetId, activeIds);

            var keys = activeIds
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
            var importCostLookup = await _uow.Imports.GetImportCostLookupByLocationAsync(context.BusinessLocationId);

            var periodMovements = allMovements.Where(sm =>
            {
                var d = DateOnly.FromDateTime(sm.CreatedAt);
                return d >= context.PeriodStart && d <= context.PeriodEnd;
            }).ToList();

            // Include ALL distinct products with any movement (even before this period),
            // so products with no activity in the current period still appear with their opening balance.
            var allProductGroups = allMovements
                .GroupBy(sm => sm.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var periodProductGroups = periodMovements
                .GroupBy(sm => sm.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var keys = allProductGroups.Keys.OrderBy(k => k).Select(k => k.ToString()).ToList();
            var names = allProductGroups.ToDictionary(
                kv => kv.Key.ToString(),
                kv => kv.Value.First().Product?.ProductName ?? $"Product #{kv.Key}");

            // amounts = total import value for this period only (0 if no imports this period)
            var amounts = allProductGroups.Keys.ToDictionary(
                pid => pid.ToString(),
                pid => periodProductGroups.TryGetValue(pid, out var moves)
                    ? moves.Where(sm => sm.Quantity > 0)
                           .Sum(sm => Math.Abs(sm.Quantity) * GetMovementCostPrice(sm, importCostLookup))
                    : 0m);

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
