using System.Text.Json;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

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
            "stock_movements" => 0m, // Phase B.2 — stock movements not yet available
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

                // Strict string match by design: TaxType token in formula must match DB token exactly.
                var rate = rates.FirstOrDefault(r =>
                    string.Equals(r.TaxType, taxType, StringComparison.Ordinal));

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
            return new FormulaTraceNode
            {
                Step = step, NodeType = "context",
                Description = $"Context: {ctxNode.GetString()}",
                ResolvedValue = 0m, Source = "runtime"
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
