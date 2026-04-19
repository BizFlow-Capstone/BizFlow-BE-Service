namespace BizFlow.Infrastructure.Services.FormulaEngine;

internal static class AstNode
{
    public const string Literal = "literal";
    public const string Ref = "ref";
    public const string Aggregate = "aggregate";
    public const string Source = "source";
    public const string Field = "field";
    public const string Filter = "filter";
    public const string PeriodFilter = "periodFilter";
    public const string Sign = "sign";
    public const string Scope = "scope";
    public const string Op = "op";
    public const string Left = "left";
    public const string Right = "right";
    public const string Fn = "fn";
    public const string Args = "args";
    public const string Foreach = "foreach";
    public const string Reduce = "reduce";
    public const string Apply = "apply";
    public const string CostSource = "costSource";
    public const string CostField = "costField";
    public const string Threshold = "threshold";
    public const string Deduction = "deduction";
    public const string Context = "context";
    public const string Lookup = "lookup";
    public const string Entity = "entity";
    public const string Min = "min";
    public const string ElseValue = "elseValue";
    public const string Amount = "amount";
    public const string Target = "target";
}

internal static class AggType
{
    public const string Sum = "SUM";
    public const string Avg = "AVG";
    public const string Count = "COUNT";
}

internal static class AggSource
{
    public const string Revenues = "revenues";
    public const string Costs = "costs";
    public const string GlEntries = "gl_entries";
    public const string StockMovements = "stock_movements";
}

internal static class OpType
{
    public const string Add = "ADD";
    public const string Subtract = "SUBTRACT";
    public const string Multiply = "MULTIPLY";
    public const string Divide = "DIVIDE";
}

internal static class FnType
{
    public const string Max = "MAX";
    public const string Min = "MIN";
    public const string Abs = "ABS";
}

internal static class ReduceType
{
    public const string Sum = "SUM";
    public const string Max = "MAX";
    public const string Min = "MIN";
}

internal static class RoundingMode
{
    public const string Floor = "floor";
    public const string Ceil = "ceil";
    public const string RoundHalfUp = "round_half_up";
}

internal static class ContextKey
{
    public const string GroupAmount = "group_amount";
    public const string GroupCost = "group_cost";
    public const string GroupDeduction = "group_deduction";
    public const string TotalAmount = "total_amount";
}

internal static class LookupEntity
{
    public const string AccountingPeriods = "AccountingPeriods";
    public const string IndustryTaxRates = "IndustryTaxRates";
}

internal static class PeriodFilter
{
    public const string Before = "before";
    public const string Current = "current";
}

internal static class SignFilter
{
    public const string Positive = "positive";
    public const string Negative = "negative";
}

internal static class GlFilterKey
{
    public const string MoneyChannel = "MoneyChannel";
    public const string TransactionType = "TransactionType";
}

internal static class DeductionTarget
{
    public const string HighestRevenue = "highest_revenue";
}
