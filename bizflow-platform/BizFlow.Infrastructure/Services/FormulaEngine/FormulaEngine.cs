using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BizFlow.Infrastructure.Services.FormulaEngine;

/// <summary>
/// Evaluates FormulaDefinition ExpressionJson AST nodes.
/// Supports: AGGREGATE, CELL_REF, TAX_RATE, WEIGHTED_AVG, EXTERNAL_LOOKUP.
/// </summary>
public class FormulaEngine : IFormulaEngine
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FormulaEngine> _logger;

    public FormulaEngine(IUnitOfWork uow, ILogger<FormulaEngine> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Dictionary<string, decimal>> EvaluateFormulasAsync(
        FormulaEvaluationContext context,
        IEnumerable<FormulaDefinition> formulas)
    {
        var results = new Dictionary<string, decimal>(context.ResolvedValues);

        // Evaluate formulas — they may depend on each other via refs.
        // Process in a deterministic order so refs resolve correctly.
        foreach (var formula in formulas)
        {
            try
            {
                var value = await EvaluateNodeAsync(context, results, formula);
                value = ApplyRounding(value, formula);
                results[formula.Code] = value;

                _logger.LogDebug("Formula {Code} = {Value}", formula.Code, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to evaluate formula {Code}, defaulting to 0", formula.Code);
                results[formula.Code] = 0m;
            }
        }

        return results;
    }

    // ────────────────────────────────────────────────────────
    // CORE AST EVALUATOR
    // ────────────────────────────────────────────────────────
    private async Task<decimal> EvaluateNodeAsync(
        FormulaEvaluationContext ctx,
        Dictionary<string, decimal> resolved,
        FormulaDefinition formula)
    {
        var json = JsonDocument.Parse(formula.ExpressionJson);
        var root = json.RootElement;
        return await EvaluateElementAsync(ctx, resolved, root);
    }

    private async Task<decimal> EvaluateElementAsync(
        FormulaEvaluationContext ctx,
        Dictionary<string, decimal> resolved,
        JsonElement node)
    {
        // 1. Literal value
        if (node.TryGetProperty("literal", out var literal))
        {
            return literal.GetDecimal();
        }

        // 2. Reference to another formula result
        if (node.TryGetProperty(AstNode.Ref, out var refNode))
        {
            var refCode = refNode.GetString()!;
            return resolved.TryGetValue(refCode, out var val) ? val : 0m;
        }

        // 3. Aggregate (SUM/AVG/COUNT from data source)
        if (node.TryGetProperty(AstNode.Aggregate, out _))
        {
            return await EvaluateAggregateAsync(ctx, node);
        }

        // 4. Lookup (EXTERNAL_LOOKUP)
        if (node.TryGetProperty(AstNode.Lookup, out var lookup))
        {
            return await EvaluateLookupAsync(ctx, lookup);
        }

        // 5. Binary operation (ADD, SUBTRACT, MULTIPLY, DIVIDE)
        if (node.TryGetProperty(AstNode.Op, out var op))
        {
            return await EvaluateOpAsync(ctx, resolved, node, op.GetString()!);
        }

        // 6. Function call (MAX, MIN, ABS)
        if (node.TryGetProperty(AstNode.Fn, out var fn))
        {
            return await EvaluateFnAsync(ctx, resolved, node, fn.GetString()!);
        }

        // 7. foreach — iterate per group, apply expression, reduce
        if (node.TryGetProperty(AstNode.Foreach, out _))
        {
            var (total, _) = await EvaluateForeachAsync(ctx, resolved, node);
            return total;
        }

        // 8. context — runtime values during foreach iteration
        if (node.TryGetProperty(AstNode.Context, out var ctxProp))
        {
            return ResolveContextValue(ctx, ctxProp.GetString());
        }

        _logger.LogWarning("Unknown AST node structure: {Node}", node.GetRawText());
        return 0m;
    }

    // ────────────────────────────────────────────────────────
    // AGGREGATE: SUM/AVG/COUNT from real data
    // ────────────────────────────────────────────────────────
    private async Task<decimal> EvaluateAggregateAsync(
        FormulaEvaluationContext ctx, JsonElement node)
    {
        var aggType = node.GetProperty(AstNode.Aggregate).GetString()!;
        var source = node.GetProperty(AstNode.Source).GetString()!;
        var field = node.GetProperty(AstNode.Field).GetString()!;

        // Build filter from JSON
        var filters = new Dictionary<string, string>();
        if (node.TryGetProperty(AstNode.Filter, out var filterNode))
        {
            foreach (var prop in filterNode.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                    filters[prop.Name] = string.Join(",", prop.Value.EnumerateArray().Select(v => v.GetString()));
                else
                    filters[prop.Name] = prop.Value.GetString() ?? "";
            }
        }

        var periodFilter = node.TryGetProperty(AstNode.PeriodFilter, out var pf) ? pf.GetString() : PeriodFilter.Current;
        var sign = node.TryGetProperty(AstNode.Sign, out var sg) ? sg.GetString() : null;

        return source switch
        {
            AggSource.Revenues => await AggregateRevenuesAsync(ctx, aggType, field, filters),
            AggSource.Costs => await AggregateCostsAsync(ctx, aggType, field, filters),
            AggSource.GlEntries => await AggregateGLAsync(ctx, aggType, field, filters),
            AggSource.StockMovements => await AggregateStockMovementsAsync(ctx, aggType, field, filters, periodFilter, sign),
            _ => 0m
        };
    }

    private async Task<decimal> AggregateRevenuesAsync(
        FormulaEvaluationContext ctx,
        string aggType, string field,
        Dictionary<string, string> filters)
    {
        var revenueTypes = filters.TryGetValue("RevenueType", out var rt)
            ? rt.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : Array.Empty<string>();

        return await _uow.Revenues.AggregateByLocationAndPeriodAsync(
            ctx.BusinessLocationId, ctx.PeriodStart, ctx.PeriodEnd, aggType, revenueTypes);
    }

    private async Task<decimal> AggregateCostsAsync(
        FormulaEvaluationContext ctx,
        string aggType, string field,
        Dictionary<string, string> filters)
    {
        return await _uow.Costs.AggregateByLocationAndPeriodAsync(
            ctx.BusinessLocationId, ctx.PeriodStart, ctx.PeriodEnd, aggType);
    }

    private async Task<decimal> AggregateGLAsync(
        FormulaEvaluationContext ctx,
        string aggType, string field,
        Dictionary<string, string> filters)
    {
        filters.TryGetValue(GlFilterKey.MoneyChannel, out var mc);
        filters.TryGetValue(GlFilterKey.TransactionType, out var tt);

        return await _uow.GeneralLedgerEntries.AggregateByLocationAndPeriodAsync(
            ctx.BusinessLocationId, ctx.PeriodStart, ctx.PeriodEnd, aggType, field, mc, tt);
    }

    private async Task<decimal> AggregateStockMovementsAsync(
        FormulaEvaluationContext ctx,
        string aggType, string field,
        Dictionary<string, string> filters,
        string? periodFilter, string? sign)
    {
        // Compute date range at DB level based on periodFilter
        var (dbFrom, dbTo) = periodFilter switch
        {
            PeriodFilter.Before => ((DateOnly?)null, (DateOnly?)ctx.PeriodStart.AddDays(-1)),
            PeriodFilter.Current => (ctx.PeriodStart, (DateOnly?)ctx.PeriodEnd),
            _ => ((DateOnly?)null, (DateOnly?)null)
        };

        long? productId = filters.TryGetValue("ProductId", out var pid) && long.TryParse(pid, out var id)
            ? id : null;

        var movements = await _uow.StockMovements.GetByLocationAndPeriodAsync(
            ctx.BusinessLocationId, dbFrom, dbTo, productId);

        // Apply sign filter in-memory (cheap after DB reduced the dataset)
        var list = sign switch
        {
            SignFilter.Positive => movements.Where(sm => sm.Quantity > 0).ToList(),
            SignFilter.Negative => movements.Where(sm => sm.Quantity < 0).ToList(),
            _ => movements
        };

        // TotalValue requires import-time cost - fetch lookup only when needed
        if (field == "TotalValue")
        {
            var importCostLookup = await _uow.Imports.GetImportCostLookupByLocationAsync(ctx.BusinessLocationId);

            decimal GetCostPrice(StockMovement sm)
            {
                if (sm.ReferenceType == StockMovementReferenceType.Import
                    && sm.ReferenceId.HasValue
                    && importCostLookup.TryGetValue((sm.ReferenceId.Value, sm.ProductId), out var ic))
                    return ic;
                return sm.Product?.CostPrice ?? 0m;
            }

            var sumVal = list.Sum(sm => Math.Abs(sm.Quantity) * GetCostPrice(sm));
            var avgVal = list.Count > 0 ? list.Average(sm => Math.Abs(sm.Quantity) * GetCostPrice(sm)) : 0m;
            return ComputeAgg(aggType, list.Count, sumVal, avgVal);
        }

        return ComputeAgg(
            aggType, list.Count,
            list.Sum(sm => GetStockFieldValue(sm, field)),
            list.Count > 0 ? list.Average(sm => GetStockFieldValue(sm, field)) : 0m);
    }

    // ────────────────────────────────────────────────────────
    // LOOKUP: External data (AccountingPeriods, IndustryTaxRates)
    // ────────────────────────────────────────────────────────
    private async Task<decimal> EvaluateLookupAsync(
        FormulaEvaluationContext ctx, JsonElement lookupNode)
    {
        var entity = lookupNode.GetProperty("entity").GetString()!;
        var field = lookupNode.GetProperty("field").GetString()!;

        switch (entity)
        {
            case LookupEntity.AccountingPeriods:
                var period = await _uow.AccountingPeriods.GetByLocationAndIdAsync(
                    ctx.BusinessLocationId, ctx.PeriodId);
                if (period == null) return 0m;
                return field switch
                {
                    "OpeningCashBalance" => period.OpeningCashBalance ?? 0m,
                    "OpeningBankBalance" => period.OpeningBankBalance ?? 0m,
                    _ => 0m
                };

            case LookupEntity.IndustryTaxRates:
                var filter = new Dictionary<string, string>();
                if (lookupNode.TryGetProperty(AstNode.Filter, out var f))
                    foreach (var p in f.EnumerateObject())
                        filter[p.Name] = p.Value.GetString() ?? "";

                var taxType = filter.GetValueOrDefault("TaxType", "VAT").Trim();

                // When inside a foreach, the current group's business type may not be in
                // ctx.BusinessTypeIds (which is product-derived). Include it explicitly so
                // manual-revenue business types still get a valid rate lookup.
                IEnumerable<Guid> lookupIds = ctx.BusinessTypeIds;
                if (ctx.CurrentBusinessTypeId.HasValue
                    && !ctx.BusinessTypeIds.Contains(ctx.CurrentBusinessTypeId.Value))
                    lookupIds = ctx.BusinessTypeIds.Append(ctx.CurrentBusinessTypeId.Value);
                var rates = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(
                    ctx.RulesetId, lookupIds);

                // When inside a foreach iteration, filter by the current business type
                IndustryTaxRate? rate;
                if (ctx.CurrentBusinessTypeId.HasValue)
                {
                    rate = rates.FirstOrDefault(r =>
                        r.BusinessTypeId == ctx.CurrentBusinessTypeId.Value
                        && string.Equals(r.TaxType, taxType, StringComparison.Ordinal));
                }
                else
                {
                    // Strict string match by design: TaxType token in formula must match DB token exactly.
                    rate = rates.FirstOrDefault(r =>
                        string.Equals(r.TaxType, taxType, StringComparison.Ordinal));
                }

                if (rate == null)
                {
                    var availableTaxTypes = string.Join(",", rates.Select(x => x.TaxType).Distinct().OrderBy(x => x));
                    _logger.LogWarning(
                        "Tax rate not found with exact TaxType match. Requested={TaxType}, RulesetId={RulesetId}, BusinessTypeCount={BusinessTypeCount}, Available=[{AvailableTaxTypes}]",
                        taxType, ctx.RulesetId, ctx.BusinessTypeIds.Count, availableTaxTypes);
                }

                return rate?.TaxRate ?? 0m;

            default:
                _logger.LogWarning("Unknown lookup entity: {Entity}", entity);
                return 0m;
        }
    }

    // ────────────────────────────────────────────────────────
    // BINARY OPS: ADD, SUBTRACT, MULTIPLY, DIVIDE
    // ────────────────────────────────────────────────────────
    private async Task<decimal> EvaluateOpAsync(
        FormulaEvaluationContext ctx,
        Dictionary<string, decimal> resolved,
        JsonElement node, string op)
    {
        var left = await EvaluateElementAsync(ctx, resolved, node.GetProperty("left"));
        var right = await EvaluateElementAsync(ctx, resolved, node.GetProperty("right"));

        return ComputeOp(op, left, right);
    }

    // ────────────────────────────────────────────────────────
    // FUNCTIONS: MAX, MIN, ABS
    // ────────────────────────────────────────────────────────
    private async Task<decimal> EvaluateFnAsync(
        FormulaEvaluationContext ctx,
        Dictionary<string, decimal> resolved,
        JsonElement node, string fn)
    {
        var args = new List<decimal>();
        if (node.TryGetProperty("args", out var argsNode))
        {
            foreach (var arg in argsNode.EnumerateArray())
            {
                args.Add(await EvaluateElementAsync(ctx, resolved, arg));
            }
        }

        return ComputeFn(fn, args);
    }

    // ────────────────────────────────────────────────────────
    // TRACE: Evaluate with step-by-step debug info
    // ────────────────────────────────────────────────────────
    public async Task<FormulaTraceResult> TraceFormulaAsync(
        FormulaEvaluationContext context,
        FormulaDefinition formula)
    {
        var resolved = new Dictionary<string, decimal>(context.ResolvedValues);
        await PrecomputeTraceDependenciesAsync(context, resolved, formula);
        var counter = new StepCounter();

        var rootTrace = await TraceElementAsync(context, resolved, JsonDocument.Parse(formula.ExpressionJson).RootElement, counter);
        var finalValue = ApplyRounding(rootTrace.ResolvedValue, formula);

        return new FormulaTraceResult
        {
            FinalValue = finalValue,
            Steps = new List<FormulaTraceNode> { rootTrace }
        };
    }

    private async Task PrecomputeTraceDependenciesAsync(
        FormulaEvaluationContext ctx,
        Dictionary<string, decimal> resolved,
        FormulaDefinition formula)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<FormulaDefinition>();
        var cache = new Dictionary<string, FormulaDefinition?>(StringComparer.OrdinalIgnoreCase);

        await CollectDependencyFormulasAsync(formula, visited, visiting, ordered, cache, includeSelf: false);

        foreach (var dep in ordered)
        {
            if (resolved.ContainsKey(dep.Code))
                continue;

            try
            {
                var value = await EvaluateNodeAsync(ctx, resolved, dep);
                value = ApplyRounding(value, dep);
                resolved[dep.Code] = value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to precompute dependency formula {Code} for trace, defaulting to 0", dep.Code);
                resolved[dep.Code] = 0m;
            }
        }
    }

    private async Task CollectDependencyFormulasAsync(
        FormulaDefinition formula,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<FormulaDefinition> ordered,
        Dictionary<string, FormulaDefinition?> cache,
        bool includeSelf)
    {
        var code = formula.Code;
        if (visited.Contains(code))
            return;

        if (!visiting.Add(code))
        {
            _logger.LogWarning("Circular formula dependency detected at {Code}", code);
            return;
        }

        foreach (var refCode in ExtractRefCodes(formula.ExpressionJson))
        {
            if (string.Equals(refCode, code, StringComparison.OrdinalIgnoreCase))
                continue;

            var dep = await GetFormulaByCodeAsync(refCode, cache);
            if (dep == null)
                continue;

            await CollectDependencyFormulasAsync(dep, visited, visiting, ordered, cache, includeSelf: true);
        }

        visiting.Remove(code);

        if (includeSelf && !visited.Contains(code))
        {
            visited.Add(code);
            ordered.Add(formula);
        }
    }

    private async Task<FormulaDefinition?> GetFormulaByCodeAsync(
        string code,
        Dictionary<string, FormulaDefinition?> cache)
    {
        if (cache.TryGetValue(code, out var cached))
            return cached;

        var formula = (await _uow.FormulaDefinitions.GetByCodesAsync(new[] { code }))
            .FirstOrDefault();
        cache[code] = formula;
        return formula;
    }

    private static IReadOnlyCollection<string> ExtractRefCodes(string? expressionJson)
    {
        if (string.IsNullOrWhiteSpace(expressionJson))
            return Array.Empty<string>();

        try
        {
            using var doc = JsonDocument.Parse(expressionJson);
            var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectRefCodes(doc.RootElement, refs);
            return refs;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static void CollectRefCodes(JsonElement node, HashSet<string> refs)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty(AstNode.Ref, out var refNode))
            {
                var code = refNode.GetString();
                if (!string.IsNullOrWhiteSpace(code))
                    refs.Add(code);
            }

            foreach (var prop in node.EnumerateObject())
            {
                CollectRefCodes(prop.Value, refs);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                CollectRefCodes(item, refs);
            }
        }
    }

    private class StepCounter { public int Value; public int Next() => ++Value; }

    private async Task<FormulaTraceNode> TraceElementAsync(
        FormulaEvaluationContext ctx,
        Dictionary<string, decimal> resolved,
        JsonElement node,
        StepCounter counter)
    {
        var step = counter.Next();

        // 1. Literal
        if (node.TryGetProperty("literal", out var literal))
        {
            var val = literal.GetDecimal();
            return new FormulaTraceNode
            {
                Step = step, NodeType = "literal",
                Description = $"Hằng số = {val}",
                ResolvedValue = val, Source = "constant"
            };
        }

        // 2. Ref
        if (node.TryGetProperty("ref", out var refNode))
        {
            var refCode = refNode.GetString()!;
            var found = resolved.TryGetValue(refCode, out var val);
            return new FormulaTraceNode
            {
                Step = step, NodeType = "ref",
                Description = $"Tham chiếu → {refCode}",
                ResolvedValue = val, Source = "formula_cache",
                Debug = found ? null : $"Chưa tìm thấy '{refCode}' trong resolved values"
            };
        }

        // 3. Aggregate
        if (node.TryGetProperty("aggregate", out _))
        {
            var aggType = node.GetProperty("aggregate").GetString()!;
            var source = node.GetProperty("source").GetString()!;
            var field = node.GetProperty("field").GetString()!;
            decimal val;
            string? debug = null;
            try
            {
                val = await EvaluateAggregateAsync(ctx, node);
            }
            catch (Exception ex)
            {
                val = 0m;
                debug = $"Error: {ex.Message}";
            }
            return new FormulaTraceNode
            {
                Step = step, NodeType = "aggregate",
                Description = $"{aggType}({source}.{field})",
                ResolvedValue = val, Source = $"DB query: {source}",
                Debug = debug
            };
        }

        // 4. Lookup
        if (node.TryGetProperty("lookup", out var lookupNode))
        {
            var entity = lookupNode.GetProperty("entity").GetString()!;
            var field = lookupNode.GetProperty("field").GetString()!;
            decimal val;
            string? debug = null;
            try
            {
                val = await EvaluateLookupAsync(ctx, lookupNode);
                if (val == 0m)
                    debug = $"Lookup returned 0 — check if matching row exists for RulesetId={ctx.RulesetId}, BusinessTypeIds=[{string.Join(",", ctx.BusinessTypeIds.Take(3))}]";
            }
            catch (Exception ex)
            {
                val = 0m;
                debug = $"Error: {ex.Message}";
            }
            return new FormulaTraceNode
            {
                Step = step, NodeType = "lookup",
                Description = $"Lookup({entity}.{field})",
                ResolvedValue = val, Source = $"DB lookup: {entity}",
                Debug = debug
            };
        }

        // 5. Op
        if (node.TryGetProperty(AstNode.Op, out var opNode))
        {
            var op = opNode.GetString()!;
            var leftTrace = await TraceElementAsync(ctx, resolved, node.GetProperty(AstNode.Left), counter);
            var rightTrace = await TraceElementAsync(ctx, resolved, node.GetProperty(AstNode.Right), counter);
            var result = ComputeOp(op, leftTrace.ResolvedValue, rightTrace.ResolvedValue);
            return new FormulaTraceNode
            {
                Step = step, NodeType = AstNode.Op,
                Description = $"{op}(left, right)",
                ResolvedValue = result, Source = "computed",
                Children = new List<FormulaTraceNode> { leftTrace, rightTrace }
            };
        }

        // 6. Function
        if (node.TryGetProperty(AstNode.Fn, out var fnNode))
        {
            var fn = fnNode.GetString()!;
            var children = new List<FormulaTraceNode>();
            var argValues = new List<decimal>();
            if (node.TryGetProperty(AstNode.Args, out var argsNode))
            {
                foreach (var arg in argsNode.EnumerateArray())
                {
                    var childTrace = await TraceElementAsync(ctx, resolved, arg, counter);
                    children.Add(childTrace);
                    argValues.Add(childTrace.ResolvedValue);
                }
            }
            var result = ComputeFn(fn, argValues);
            return new FormulaTraceNode
            {
                Step = step, NodeType = AstNode.Fn,
                Description = $"{fn}({string.Join(", ", argValues)})",
                ResolvedValue = result, Source = "computed",
                Children = children
            };
        }

        // 7. Context
        if (node.TryGetProperty(AstNode.Context, out var ctxNode))
        {
            var ctxKey = ctxNode.GetString();
            var ctxVal = ResolveContextValue(ctx, ctxKey);
            return new FormulaTraceNode
            {
                Step = step, NodeType = AstNode.Context,
                Description = $"Context: {ctxKey}",
                ResolvedValue = ctxVal, Source = "runtime"
            };
        }

        // 8. foreach
        if (node.TryGetProperty(AstNode.Foreach, out _))
        {
            var (total, breakdown) = await EvaluateForeachAsync(ctx, resolved, node);
            var children = breakdown.Select(kv => new FormulaTraceNode
            {
                Step = counter.Next(), NodeType = "foreach_group",
                Description = $"Group {kv.Key}",
                ResolvedValue = kv.Value, Source = $"BusinessTypeId={kv.Key}"
            }).ToList();
            return new FormulaTraceNode
            {
                Step = step, NodeType = AstNode.Foreach,
                Description = $"foreach({node.GetProperty(AstNode.Foreach).GetString()}) → {(node.TryGetProperty(AstNode.Reduce, out var red) ? red.GetString() : ReduceType.Sum)}",
                ResolvedValue = total, Source = "iteration",
                Children = children.Count > 0 ? children : null,
                Debug = breakdown.Count == 0 ? "No groups found" : null
            };
        }

        return new FormulaTraceNode
        {
            Step = step, NodeType = "unknown",
            Description = "Unrecognized node",
            ResolvedValue = 0m, Debug = node.GetRawText()
        };
    }

    // ────────────────────────────────────────────────────────
    // FIELD VALUE EXTRACTORS
    // ────────────────────────────────────────────────────────
    private static decimal GetRevenueFieldValue(Revenue r, string field) =>
        field switch
        {
            "Amount" => r.Amount,
            _ => 0m
        };

    private static decimal GetCostFieldValue(Cost c, string field) =>
        field switch
        {
            "Amount" => c.Amount,
            _ => 0m
        };

    private static decimal GetGLFieldValue(GeneralLedgerEntry e, string field) =>
        field switch
        {
            "DebitAmount" => e.DebitAmount,
            "CreditAmount" => e.CreditAmount,
            _ => 0m
        };

    private static decimal GetStockFieldValue(StockMovement sm, string field) =>
        field switch
        {
            "QuantityDelta" or "Quantity" => sm.Quantity,
            "TotalValue" => Math.Abs(sm.Quantity) * (sm.Product?.CostPrice ?? 0m),
            _ => 0m
        };

    // ────────────────────────────────────────────────────────
    // FOREACH: Iterate per industry/group, apply expression, reduce
    // ────────────────────────────────────────────────────────
    private async Task<(decimal Total, Dictionary<string, decimal> Breakdown)> EvaluateForeachAsync(
        FormulaEvaluationContext ctx,
        Dictionary<string, decimal> resolved,
        JsonElement node)
    {
        var source = node.GetProperty(AstNode.Source).GetString()!;
        var field = node.GetProperty(AstNode.Field).GetString()!;
        var reduce = node.TryGetProperty(AstNode.Reduce, out var r) ? r.GetString()! : ReduceType.Sum;
        var applyNode = node.GetProperty(AstNode.Apply);

        // Optional cost source for profit-based formulas
        var hasCostSource = node.TryGetProperty(AstNode.CostSource, out var costSrcProp);
        var costSource = hasCostSource ? costSrcProp.GetString() : null;
        var costField = node.TryGetProperty(AstNode.CostField, out var cf) ? cf.GetString()! : "Amount";

        // Load grouped amounts
        var revenueByGroup = await LoadGroupedAmountsAsync(ctx, source, field);
        var costByGroup = hasCostSource && !string.IsNullOrEmpty(costSource)
            ? await LoadGroupedAmountsAsync(ctx, costSource, costField)
            : null;

        var totalAmount = revenueByGroup.Values.Sum();

        // Optional threshold check (legacy — kept for backward compatibility)
        if (node.TryGetProperty(AstNode.Threshold, out var thresholdNode))
        {
            var minValue = thresholdNode.GetProperty(AstNode.Min).GetDecimal();
            var elseValue = thresholdNode.TryGetProperty(AstNode.ElseValue, out var ev) ? ev.GetDecimal() : 0m;

            if (totalAmount <= minValue)
            {
                _logger.LogDebug("foreach threshold not met: {Total} <= {Min}, returning {ElseValue}",
                    totalAmount, minValue, elseValue);
                return (elseValue, new Dictionary<string, decimal>());
            }
        }

        // Optional deduction: subtract a fixed amount from the highest-revenue group
        // The apply expression uses {context: "group_deduction"} to access this value.
        var deductionAmount = 0m;
        string? deductionTargetGroupKey = null;
        if (node.TryGetProperty(AstNode.Deduction, out var deductionNode))
        {
            deductionAmount = deductionNode.GetProperty(AstNode.Amount).GetDecimal();
            var target = deductionNode.TryGetProperty(AstNode.Target, out var t) ? t.GetString() : DeductionTarget.HighestRevenue;

            if (target == DeductionTarget.HighestRevenue && revenueByGroup.Count > 0)
            {
                deductionTargetGroupKey = revenueByGroup.MaxBy(kv => kv.Value).Key;
            }
        }

        // Iterate each group
        var breakdown = new Dictionary<string, decimal>();
        var groupValues = new List<decimal>();

        // Save original context values to restore after iteration
        var origBtId = ctx.CurrentBusinessTypeId;
        var origGroupAmount = ctx.GroupAmount;
        var origGroupCost = ctx.GroupCost;
        var origGroupDeduction = ctx.GroupDeduction;
        var origTotalAmount = ctx.TotalAmount;

        try
        {
            ctx.TotalAmount = totalAmount;

            foreach (var (groupKey, groupAmount) in revenueByGroup)
            {
                ctx.CurrentBusinessTypeId = Guid.TryParse(groupKey, out var gid) ? gid : null;
                ctx.GroupAmount = groupAmount;
                ctx.GroupCost = costByGroup?.GetValueOrDefault(groupKey) ?? 0m;
                ctx.GroupDeduction = groupKey == deductionTargetGroupKey ? deductionAmount : 0m;

                var groupResult = await EvaluateElementAsync(ctx, resolved, applyNode);
                breakdown[groupKey] = groupResult;
                groupValues.Add(groupResult);
            }
        }
        finally
        {
            // Restore context
            ctx.CurrentBusinessTypeId = origBtId;
            ctx.GroupAmount = origGroupAmount;
            ctx.GroupCost = origGroupCost;
            ctx.GroupDeduction = origGroupDeduction;
            ctx.TotalAmount = origTotalAmount;
        }

        // Reduce
        var total = reduce.ToUpper() switch
        {
            ReduceType.Sum => groupValues.Sum(),
            ReduceType.Max => groupValues.Count > 0 ? groupValues.Max() : 0m,
            ReduceType.Min => groupValues.Count > 0 ? groupValues.Min() : 0m,
            _ => groupValues.Sum()
        };

        return (total, breakdown);
    }

    private async Task<Dictionary<string, decimal>> LoadGroupedAmountsAsync(
        FormulaEvaluationContext ctx, string source, string field)
    {
        return source switch
        {
            AggSource.Revenues => await LoadRevenueGroupedByBusinessTypeAsync(ctx, field),
            AggSource.Costs => await LoadCostGroupedByBusinessTypeAsync(ctx, field),
            _ => new Dictionary<string, decimal>()
        };
    }

    private async Task<Dictionary<string, decimal>> LoadRevenueGroupedByBusinessTypeAsync(
        FormulaEvaluationContext ctx, string field)
    {
        return await _uow.Revenues.SumGroupedByBusinessTypeAsync(
            ctx.BusinessLocationId, ctx.PeriodStart, ctx.PeriodEnd);
    }

    private async Task<Dictionary<string, decimal>> LoadCostGroupedByBusinessTypeAsync(
        FormulaEvaluationContext ctx, string field)
    {
        return await _uow.Costs.SumGroupedByBusinessTypeAsync(
            ctx.BusinessLocationId, ctx.PeriodStart, ctx.PeriodEnd);
    }

    // ────────────────────────────────────────────────────────
    // CONTEXT: Runtime values (used inside foreach)
    // ────────────────────────────────────────────────────────
    private static decimal ResolveContextValue(FormulaEvaluationContext ctx, string? key)
    {
        return key switch
        {
            ContextKey.GroupAmount => ctx.GroupAmount ?? 0m,
            ContextKey.GroupCost => ctx.GroupCost ?? 0m,
            ContextKey.GroupDeduction => ctx.GroupDeduction ?? 0m,
            ContextKey.TotalAmount => ctx.TotalAmount ?? 0m,
            _ => 0m
        };
    }

    // ────────────────────────────────────────────────────────
    // EVALUATE WITH BREAKDOWN (foreach-aware)
    // ────────────────────────────────────────────────────────
    public async Task<FormulaEvaluationResults> EvaluateFormulasWithBreakdownAsync(
        FormulaEvaluationContext context,
        IEnumerable<FormulaDefinition> formulas)
    {
        var results = new FormulaEvaluationResults();
        results.Values = new Dictionary<string, decimal>(context.ResolvedValues);

        foreach (var formula in formulas)
        {
            try
            {
                var json = JsonDocument.Parse(formula.ExpressionJson);
                var root = json.RootElement;

                // Check if root node is a foreach — if so, capture breakdown
                if (root.TryGetProperty(AstNode.Foreach, out _))
                {
                    var (total, breakdown) = await EvaluateForeachAsync(context, results.Values, root);
                    total = ApplyRounding(total, formula);
                    results.Values[formula.Code] = total;
                    results.Breakdowns[formula.Code] = breakdown;
                }
                else
                {
                    var value = await EvaluateElementAsync(context, results.Values, root);
                    value = ApplyRounding(value, formula);
                    results.Values[formula.Code] = value;
                }

                _logger.LogDebug("Formula {Code} = {Value}", formula.Code, results.Values[formula.Code]);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to evaluate formula {Code}, defaulting to 0", formula.Code);
                results.Values[formula.Code] = 0m;
            }
        }

        return results;
    }

    // ────────────────────────────────────────────────────────
    // SHARED COMPUTE HELPERS (eliminates duplication between evaluate and trace paths)
    // ────────────────────────────────────────────────────────
    private static decimal ComputeOp(string op, decimal left, decimal right) =>
        op.ToUpper() switch
        {
            OpType.Add => left + right,
            OpType.Subtract => left - right,
            OpType.Multiply => left * right,
            OpType.Divide => right != 0 ? left / right : 0m,
            _ => 0m
        };

    private static decimal ComputeFn(string fn, List<decimal> args) =>
        fn.ToUpper() switch
        {
            FnType.Max => args.Count > 0 ? args.Max() : 0m,
            FnType.Min => args.Count > 0 ? args.Min() : 0m,
            FnType.Abs => args.Count > 0 ? Math.Abs(args[0]) : 0m,
            _ => 0m
        };

    private static decimal ComputeAgg(string aggType, int count, decimal sum, decimal avg) =>
        aggType.ToUpper() switch
        {
            AggType.Sum => sum,
            AggType.Avg => avg,
            AggType.Count => count,
            _ => 0m
        };

    // ────────────────────────────────────────────────────────
    // ROUNDING
    // ────────────────────────────────────────────────────────
    private static decimal ApplyRounding(decimal value, FormulaDefinition formula)
    {
        if (string.IsNullOrEmpty(formula.RoundingMode))
            return value;

        var precision = formula.RoundingPrecision;

        return formula.RoundingMode switch
        {
            RoundingMode.Floor => Math.Floor(value * (decimal)Math.Pow(10, precision)) / (decimal)Math.Pow(10, precision),
            RoundingMode.Ceil => Math.Ceiling(value * (decimal)Math.Pow(10, precision)) / (decimal)Math.Pow(10, precision),
            RoundingMode.RoundHalfUp => Math.Round(value, precision, MidpointRounding.AwayFromZero),
            _ => Math.Round(value, precision)
        };
    }
}
