using System;

namespace BizFlow.Domain.Entities;

public partial class TaxGroupRule
{
    public int RuleId { get; set; }
    public int RulesetId { get; set; }

    public int GroupNumber { get; set; }
    public string GroupName { get; set; } = null!;
    public string? GroupDescription { get; set; }

    /// <summary>
    /// JSON: {"minRevenue":500000000,"maxRevenue":3000000000}
    /// </summary>
    public string ConditionsJson { get; set; } = null!;

    /// <summary>
    /// JSON: {"vatExempt":false,"allowedTaxMethods":["method_1","method_2"],...}
    /// </summary>
    public string OutcomesJson { get; set; } = null!;

    public int SortOrder { get; set; }

    // Navigation
    public virtual TaxRuleset Ruleset { get; set; } = null!;
}
