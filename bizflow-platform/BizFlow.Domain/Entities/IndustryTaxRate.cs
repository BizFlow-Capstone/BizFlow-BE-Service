using System;

namespace BizFlow.Domain.Entities;

public partial class IndustryTaxRate
{
    public int RateId { get; set; }
    public int RulesetId { get; set; }
    public Guid BusinessTypeId { get; set; }

    /// <summary>
    /// VAT | PIT_METHOD_1
    /// </summary>
    public string TaxType { get; set; } = null!;

    public decimal TaxRate { get; set; }
    public string? Description { get; set; }

    // Navigation
    public virtual TaxRuleset Ruleset { get; set; } = null!;
    public virtual BusinessType BusinessType { get; set; } = null!;
}
