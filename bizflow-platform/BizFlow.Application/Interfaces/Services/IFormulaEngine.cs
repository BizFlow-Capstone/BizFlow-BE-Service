namespace BizFlow.Application.Interfaces.Services;

/// <summary>
/// Evaluate FormulaDefinition ExpressionJson AST nodes.
/// Returns cached results when available, computes + caches when stale.
/// </summary>
public interface IFormulaEngine
{
    /// <summary>
    /// Evaluate a set of formulas for a book context.
    /// Results are keyed by FormulaDefinition.Code.
    /// </summary>
    Task<Dictionary<string, decimal>> EvaluateFormulasAsync(
        FormulaEvaluationContext context,
        IEnumerable<Domain.Entities.FormulaDefinition> formulas);

    /// <summary>
    /// Evaluate formulas and return both scalar totals and per-group breakdowns.
    /// Breakdowns are populated for formulas that use foreach nodes.
    /// </summary>
    Task<FormulaEvaluationResults> EvaluateFormulasWithBreakdownAsync(
        FormulaEvaluationContext context,
        IEnumerable<Domain.Entities.FormulaDefinition> formulas);

    /// <summary>
    /// Evaluate a single formula with full execution trace for debugging.
    /// </summary>
    Task<FormulaTraceResult> TraceFormulaAsync(
        FormulaEvaluationContext context,
        Domain.Entities.FormulaDefinition formula);
}

public class FormulaTraceResult
{
    public decimal FinalValue { get; set; }
    public List<FormulaTraceNode> Steps { get; set; } = new();
}

public class FormulaTraceNode
{
    public int Step { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ResolvedValue { get; set; }
    public string? Source { get; set; }
    public string? Debug { get; set; }
    public List<FormulaTraceNode>? Children { get; set; }
}

public class FormulaEvaluationContext
{
    public long BookId { get; set; }
    public int BusinessLocationId { get; set; }
    public long PeriodId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int RulesetId { get; set; }
    public List<Guid> BusinessTypeIds { get; set; } = new();

    /// <summary>
    /// Pre-computed intermediate values (e.g., formula refs → results).
    /// </summary>
    public Dictionary<string, decimal> ResolvedValues { get; set; } = new();

    // ── foreach iteration context (set by engine during per-group evaluation) ──

    /// <summary>Current BusinessTypeId when iterating inside a foreach node.</summary>
    public Guid? CurrentBusinessTypeId { get; set; }

    /// <summary>Revenue/amount of the current group within a foreach iteration.</summary>
    public decimal? GroupAmount { get; set; }

    /// <summary>Cost of the current group within a foreach iteration.</summary>
    public decimal? GroupCost { get; set; }

    /// <summary>Total amount across all groups (for threshold checks).</summary>
    public decimal? TotalAmount { get; set; }
}

/// <summary>
/// Extended evaluation results that include per-group breakdowns from foreach formulas.
/// </summary>
public class FormulaEvaluationResults
{
    /// <summary>Formula Code → scalar total value.</summary>
    public Dictionary<string, decimal> Values { get; set; } = new();

    /// <summary>Formula Code → (GroupKey → value). Populated only for formulas using foreach nodes.</summary>
    public Dictionary<string, Dictionary<string, decimal>> Breakdowns { get; set; } = new();
}
