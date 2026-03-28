using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class TaxRuleset
{
    public int RulesetId { get; set; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Version { get; set; } = null!;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual ICollection<TaxGroupRule> GroupRules { get; set; } = new List<TaxGroupRule>();
    public virtual ICollection<IndustryTaxRate> IndustryTaxRates { get; set; } = new List<IndustryTaxRate>();
    public virtual ICollection<AccountingBook> AccountingBooks { get; set; } = new List<AccountingBook>();
}
