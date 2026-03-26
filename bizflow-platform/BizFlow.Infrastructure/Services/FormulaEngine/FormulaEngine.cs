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

        if (filters.TryGetValue("RevenueType", out var rt))
            query.RevenueType = rt.Split(',').First();

        var (items, _) = await _uow.Revenues.SearchAsync(query);
        var list = items.Where(r => r.DeletedAt == null).ToList();

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

                var taxType = filter.GetValueOrDefault("TaxType", "VAT");
                var rates = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(
                    ctx.RulesetId, ctx.BusinessTypeIds);

                // Return the first matching rate (all business types in this book share same rate)
                var rate = rates.FirstOrDefault(r => r.TaxType == taxType);
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
