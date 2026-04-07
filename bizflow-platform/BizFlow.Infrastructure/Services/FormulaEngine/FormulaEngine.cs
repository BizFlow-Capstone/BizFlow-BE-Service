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
        if (node.TryGetProperty("ref", out var refNode))
        {
            var refCode = refNode.GetString()!;
            return resolved.TryGetValue(refCode, out var val) ? val : 0m;
        }

        // 3. Aggregate (SUM/AVG/COUNT from data source)
        if (node.TryGetProperty("aggregate", out var aggregate))
        {
            return await EvaluateAggregateAsync(ctx, node);
        }

        // 4. Lookup (EXTERNAL_LOOKUP)
        if (node.TryGetProperty("lookup", out var lookup))
        {
            return await EvaluateLookupAsync(ctx, lookup);
        }

        // 5. Binary operation (ADD, SUBTRACT, MULTIPLY, DIVIDE)
        if (node.TryGetProperty("op", out var op))
        {
            return await EvaluateOpAsync(ctx, resolved, node, op.GetString()!);
        }

        // 6. Function call (MAX, MIN, ABS)
        if (node.TryGetProperty("fn", out var fn))
        {
            return await EvaluateFnAsync(ctx, resolved, node, fn.GetString()!);
        }

        // 7. foreach — iterate per industry/group, apply expression, reduce
        if (node.TryGetProperty("foreach", out _))
        {
            var (total, _) = await EvaluateForeachAsync(ctx, resolved, node);
            return total;
        }

        // 8. context — runtime values during foreach iteration
        if (node.TryGetProperty("context", out var ctxProp))
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
        var aggType = node.GetProperty("aggregate").GetString()!; // SUM, AVG, COUNT
        var source = node.GetProperty("source").GetString()!;     // revenues, costs, gl_entries, stock_movements
        var field = node.GetProperty("field").GetString()!;       // Amount, DebitAmount, CreditAmount...
        var scope = node.TryGetProperty("scope", out var s) ? s.GetString() : "location";

        // Build filter from JSON
        var filters = new Dictionary<string, string>();
        if (node.TryGetProperty("filter", out var filterNode))
        {
            foreach (var prop in filterNode.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                    filters[prop.Name] = string.Join(",", prop.Value.EnumerateArray().Select(v => v.GetString()));
                else
                    filters[prop.Name] = prop.Value.GetString() ?? "";
            }
        }

        // Period filter for stock movements
        var periodFilter = node.TryGetProperty("periodFilter", out var pf) ? pf.GetString() : "current";
        var sign = node.TryGetProperty("sign", out var sg) ? sg.GetString() : null;

        // Query data based on source
        return source switch
        {
            "revenues" => await AggregateRevenuesAsync(ctx, aggType, field, filters),
            "costs" => await AggregateCostsAsync(ctx, aggType, field, filters),
            "gl_entries" => await AggregateGLAsync(ctx, aggType, field, filters),
            "stock_movements" => await AggregateStockMovementsAsync(ctx, aggType, field, filters, periodFilter, sign),
            _ => 0m
        };
    }

    private async Task<decimal> AggregateRevenuesAsync(
        FormulaEvaluationContext ctx,
        string aggType, string field,
        Dictionary<string, string> filters)
    {
        var query = new Application.DTOs.Revenue.RevenueQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        // RevenueType filter may contain multiple comma-separated values (e.g. "sale,manual").
        // RevenueQueryParams only supports a single value, so we filter in-memory for multi-value.
        var revenueTypes = filters.TryGetValue("RevenueType", out var rt)
            ? rt.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : Array.Empty<string>();

        if (revenueTypes.Length == 1)
            query.RevenueType = revenueTypes[0];

        var (items, _) = await _uow.Revenues.SearchAsync(query);
        var list = items.Where(r => r.DeletedAt == null).ToList();

        if (revenueTypes.Length > 1)
            list = list.Where(r => revenueTypes.Contains(r.RevenueType, StringComparer.OrdinalIgnoreCase)).ToList();

        return aggType.ToUpper() switch
        {
            "SUM" => list.Sum(r => GetRevenueFieldValue(r, field)),
            "AVG" => list.Count > 0 ? list.Average(r => GetRevenueFieldValue(r, field)) : 0m,
            "COUNT" => list.Count,
            _ => 0m
        };
    }

    private async Task<decimal> AggregateCostsAsync(
        FormulaEvaluationContext ctx,
        string aggType, string field,
        Dictionary<string, string> filters)
    {
        var query = new Application.DTOs.Cost.CostQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (items, _) = await _uow.Costs.SearchAsync(query);
        var list = items.Where(c => c.DeletedAt == null).ToList();

        return aggType.ToUpper() switch
        {
            "SUM" => list.Sum(c => GetCostFieldValue(c, field)),
            "AVG" => list.Count > 0 ? list.Average(c => GetCostFieldValue(c, field)) : 0m,
            "COUNT" => list.Count,
            _ => 0m
        };
    }

    private async Task<decimal> AggregateGLAsync(
        FormulaEvaluationContext ctx,
        string aggType, string field,
        Dictionary<string, string> filters)
    {
        var query = new Application.DTOs.GeneralLedger.GeneralLedgerQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        if (filters.TryGetValue("MoneyChannel", out var mc))
            query.MoneyChannels = new List<string> { mc };
        if (filters.TryGetValue("TransactionType", out var tt))
            query.TransactionTypes = new List<string> { tt };

        var (items, _) = await _uow.GeneralLedgerEntries.SearchAsync(query);
        var list = items.Where(e => !e.IsReversal).ToList();

        return aggType.ToUpper() switch
        {
            "SUM" => list.Sum(e => GetGLFieldValue(e, field)),
            "AVG" => list.Count > 0 ? list.Average(e => GetGLFieldValue(e, field)) : 0m,
            "COUNT" => list.Count,
            _ => 0m
        };
    }

    private async Task<decimal> AggregateStockMovementsAsync(
        FormulaEvaluationContext ctx,
        string aggType, string field,
        Dictionary<string, string> filters,
        string? periodFilter, string? sign)
    {
        var allMovements = await _uow.StockMovements.GetByLocationAsync(ctx.BusinessLocationId);
        var importCostLookup = await _uow.Imports.GetImportCostLookupByLocationAsync(ctx.BusinessLocationId);

        // Apply periodFilter
        IEnumerable<StockMovement> filtered = periodFilter switch
        {
            "before" => allMovements.Where(sm =>
                DateOnly.FromDateTime(sm.CreatedAt) < ctx.PeriodStart),
            "current" => allMovements.Where(sm =>
            {
                var d = DateOnly.FromDateTime(sm.CreatedAt);
                return d >= ctx.PeriodStart && d <= ctx.PeriodEnd;
            }),
            _ => allMovements
        };

        // Apply sign filter
        filtered = sign switch
        {
            "positive" => filtered.Where(sm => sm.Quantity > 0),
            "negative" => filtered.Where(sm => sm.Quantity < 0),
            _ => filtered
        };

        // Apply ProductId filter if present (for per-product scope via context)
        if (filters.TryGetValue("ProductId", out var pid) && long.TryParse(pid, out var productId))
            filtered = filtered.Where(sm => sm.ProductId == productId);

        var list = filtered.ToList();

        // Resolve import-time cost price for TotalValue (don’t use Product.CostPrice which reflects last-known price)
        decimal GetCostPrice(StockMovement sm)
        {
            if (sm.ReferenceType == StockMovementReferenceType.Import
                && sm.ReferenceId.HasValue
                && importCostLookup.TryGetValue((sm.ReferenceId.Value, sm.ProductId), out var ic))
                return ic;
            return sm.Product?.CostPrice ?? 0m;
        }

        return aggType.ToUpper() switch
        {
            "SUM" => field == "TotalValue"
                ? list.Sum(sm => Math.Abs(sm.Quantity) * GetCostPrice(sm))
                : list.Sum(sm => GetStockFieldValue(sm, field)),
            "AVG" => list.Count > 0
                ? (field == "TotalValue"
                    ? list.Average(sm => Math.Abs(sm.Quantity) * GetCostPrice(sm))
                    : list.Average(sm => GetStockFieldValue(sm, field)))
                : 0m,
            "COUNT" => list.Count,
            _ => 0m
        };
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
            case "AccountingPeriods":
                var period = await _uow.AccountingPeriods.GetByLocationAndIdAsync(
                    ctx.BusinessLocationId, ctx.PeriodId);
                if (period == null) return 0m;
                return field switch
                {
                    "OpeningCashBalance" => period.OpeningCashBalance ?? 0m,
                    "OpeningBankBalance" => period.OpeningBankBalance ?? 0m,
                    _ => 0m
                };

            case "IndustryTaxRates":
                var filter = new Dictionary<string, string>();
                if (lookupNode.TryGetProperty("filter", out var f))
                    foreach (var p in f.EnumerateObject())
                        filter[p.Name] = p.Value.GetString() ?? "";

                var taxType = filter.GetValueOrDefault("TaxType", "VAT").Trim();
                var rates = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(
                    ctx.RulesetId, ctx.BusinessTypeIds);

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

        return op.ToUpper() switch
        {
            "ADD" => left + right,
            "SUBTRACT" => left - right,
            "MULTIPLY" => left * right,
            "DIVIDE" => right != 0 ? left / right : 0m,
            _ => 0m
        };
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

        return fn.ToUpper() switch
        {
            "MAX" => args.Count > 0 ? args.Max() : 0m,
            "MIN" => args.Count > 0 ? args.Min() : 0m,
            "ABS" => args.Count > 0 ? Math.Abs(args[0]) : 0m,
            _ => 0m
        };
    }

    // ────────────────────────────────────────────────────────
    // TRACE: Evaluate with step-by-step debug info
    // ────────────────────────────────────────────────────────
    public async Task<FormulaTraceResult> TraceFormulaAsync(
        FormulaEvaluationContext context,
        FormulaDefinition formula)
    {
        var resolved = new Dictionary<string, decimal>(context.ResolvedValues);
        var counter = new StepCounter();

        var rootTrace = await TraceElementAsync(context, resolved, JsonDocument.Parse(formula.ExpressionJson).RootElement, counter);
        var finalValue = ApplyRounding(rootTrace.ResolvedValue, formula);

        return new FormulaTraceResult
        {
            FinalValue = finalValue,
            Steps = new List<FormulaTraceNode> { rootTrace }
        };
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
        if (node.TryGetProperty("op", out var opNode))
        {
            var op = opNode.GetString()!;
            var leftTrace = await TraceElementAsync(ctx, resolved, node.GetProperty("left"), counter);
            var rightTrace = await TraceElementAsync(ctx, resolved, node.GetProperty("right"), counter);
            var result = op.ToUpper() switch
            {
                "ADD" => leftTrace.ResolvedValue + rightTrace.ResolvedValue,
                "SUBTRACT" => leftTrace.ResolvedValue - rightTrace.ResolvedValue,
                "MULTIPLY" => leftTrace.ResolvedValue * rightTrace.ResolvedValue,
                "DIVIDE" => rightTrace.ResolvedValue != 0 ? leftTrace.ResolvedValue / rightTrace.ResolvedValue : 0m,
                _ => 0m
            };
            return new FormulaTraceNode
            {
                Step = step, NodeType = "op",
                Description = $"{op}(left, right)",
                ResolvedValue = result, Source = "computed",
                Children = new List<FormulaTraceNode> { leftTrace, rightTrace }
            };
        }

        // 6. Function
        if (node.TryGetProperty("fn", out var fnNode))
        {
            var fn = fnNode.GetString()!;
            var children = new List<FormulaTraceNode>();
            var argValues = new List<decimal>();
            if (node.TryGetProperty("args", out var argsNode))
            {
                foreach (var arg in argsNode.EnumerateArray())
                {
                    var childTrace = await TraceElementAsync(ctx, resolved, arg, counter);
                    children.Add(childTrace);
                    argValues.Add(childTrace.ResolvedValue);
                }
            }
            var result = fn.ToUpper() switch
            {
                "MAX" => argValues.Count > 0 ? argValues.Max() : 0m,
                "MIN" => argValues.Count > 0 ? argValues.Min() : 0m,
                "ABS" => argValues.Count > 0 ? Math.Abs(argValues[0]) : 0m,
                _ => 0m
            };
            return new FormulaTraceNode
            {
                Step = step, NodeType = "fn",
                Description = $"{fn}({string.Join(", ", argValues)})",
                ResolvedValue = result, Source = "computed",
                Children = children
            };
        }

        // 7. Context
        if (node.TryGetProperty("context", out var ctxNode))
        {
            var ctxKey = ctxNode.GetString();
            var ctxVal = ResolveContextValue(ctx, ctxKey);
            return new FormulaTraceNode
            {
                Step = step, NodeType = "context",
                Description = $"Context: {ctxKey}",
                ResolvedValue = ctxVal, Source = "runtime"
            };
        }

        // 8. foreach
        if (node.TryGetProperty("foreach", out _))
        {
            var (total, breakdown) = await EvaluateForeachAsync(ctx, resolved, node);
            var children = new List<FormulaTraceNode>();
            var childStep = 0;
            foreach (var (groupKey, groupVal) in breakdown)
            {
                childStep++;
                children.Add(new FormulaTraceNode
                {
                    Step = counter.Next(), NodeType = "foreach_group",
                    Description = $"Group {groupKey}",
                    ResolvedValue = groupVal, Source = $"BusinessTypeId={groupKey}"
                });
            }
            return new FormulaTraceNode
            {
                Step = step, NodeType = "foreach",
                Description = $"foreach({node.GetProperty("foreach").GetString()}) → {(node.TryGetProperty("reduce", out var red) ? red.GetString() : "SUM")}",
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
        var dimension = node.GetProperty("foreach").GetString()!; // "industry"
        var source = node.GetProperty("source").GetString()!;     // "revenues", "costs"
        var field = node.GetProperty("field").GetString()!;       // "Amount"
        var reduce = node.TryGetProperty("reduce", out var r) ? r.GetString()! : "SUM";
        var applyNode = node.GetProperty("apply");

        // Optional cost source for profit-based formulas
        var hasCostSource = node.TryGetProperty("costSource", out var costSrcProp);
        var costSource = hasCostSource ? costSrcProp.GetString() : null;
        var costField = node.TryGetProperty("costField", out var cf) ? cf.GetString()! : "Amount";

        // Load grouped amounts
        var revenueByGroup = await LoadGroupedAmountsAsync(ctx, source, field);
        var costByGroup = hasCostSource && !string.IsNullOrEmpty(costSource)
            ? await LoadGroupedAmountsAsync(ctx, costSource, costField)
            : null;

        var totalAmount = revenueByGroup.Values.Sum();

        // Optional threshold check (legacy — kept for backward compatibility)
        if (node.TryGetProperty("threshold", out var thresholdNode))
        {
            var minValue = thresholdNode.GetProperty("min").GetDecimal();
            var elseValue = thresholdNode.TryGetProperty("elseValue", out var ev) ? ev.GetDecimal() : 0m;

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
        if (node.TryGetProperty("deduction", out var deductionNode))
        {
            deductionAmount = deductionNode.GetProperty("amount").GetDecimal();
            var target = deductionNode.TryGetProperty("target", out var t) ? t.GetString() : "highest_revenue";

            if (target == "highest_revenue" && revenueByGroup.Count > 0)
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
            "SUM" => groupValues.Sum(),
            "MAX" => groupValues.Count > 0 ? groupValues.Max() : 0m,
            "MIN" => groupValues.Count > 0 ? groupValues.Min() : 0m,
            _ => groupValues.Sum()
        };

        return (total, breakdown);
    }

    private async Task<Dictionary<string, decimal>> LoadGroupedAmountsAsync(
        FormulaEvaluationContext ctx, string source, string field)
    {
        return source switch
        {
            "revenues" => await LoadRevenueGroupedByBusinessTypeAsync(ctx, field),
            "costs" => await LoadCostGroupedByBusinessTypeAsync(ctx, field),
            _ => new Dictionary<string, decimal>()
        };
    }

    private async Task<Dictionary<string, decimal>> LoadRevenueGroupedByBusinessTypeAsync(
        FormulaEvaluationContext ctx, string field)
    {
        var query = new Application.DTOs.Revenue.RevenueQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (items, _) = await _uow.Revenues.SearchAsync(query);
        return items
            .Where(r => r.DeletedAt == null && r.BusinessTypeId.HasValue)
            .GroupBy(r => r.BusinessTypeId!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(x => GetRevenueFieldValue(x, field)));
    }

    private async Task<Dictionary<string, decimal>> LoadCostGroupedByBusinessTypeAsync(
        FormulaEvaluationContext ctx, string field)
    {
        var query = new Application.DTOs.Cost.CostQueryParams
        {
            BusinessLocationId = ctx.BusinessLocationId,
            FromDate = ctx.PeriodStart,
            ToDate = ctx.PeriodEnd,
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (items, _) = await _uow.Costs.SearchAsync(query);
        return items
            .Where(c => c.DeletedAt == null && c.BusinessTypeId.HasValue)
            .GroupBy(c => c.BusinessTypeId!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(x => GetCostFieldValue(x, field)));
    }

    // ────────────────────────────────────────────────────────
    // CONTEXT: Runtime values (used inside foreach)
    // ────────────────────────────────────────────────────────
    private static decimal ResolveContextValue(FormulaEvaluationContext ctx, string? key)
    {
        return key switch
        {
            "group_amount" => ctx.GroupAmount ?? 0m,
            "group_cost" => ctx.GroupCost ?? 0m,
            "group_deduction" => ctx.GroupDeduction ?? 0m,
            "total_amount" => ctx.TotalAmount ?? 0m,
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
                if (root.TryGetProperty("foreach", out _))
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
    // ROUNDING
    // ────────────────────────────────────────────────────────
    private static decimal ApplyRounding(decimal value, FormulaDefinition formula)
    {
        if (string.IsNullOrEmpty(formula.RoundingMode))
            return value;

        var precision = formula.RoundingPrecision;

        return formula.RoundingMode switch
        {
            "floor" => Math.Floor(value * (decimal)Math.Pow(10, precision)) / (decimal)Math.Pow(10, precision),
            "ceil" => Math.Ceiling(value * (decimal)Math.Pow(10, precision)) / (decimal)Math.Pow(10, precision),
            "round_half_up" => Math.Round(value, precision, MidpointRounding.AwayFromZero),
            _ => Math.Round(value, precision)
        };
    }
}
