using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// AccountingBook — live view config instance (does NOT store data).
/// When Owner views, system queries data realtime from GL/Orders/Costs/etc.
/// </summary>
public partial class AccountingBook
{
    public long BookId { get; set; }
    public int BusinessLocationId { get; set; }
    public long PeriodId { get; set; }
    public int TemplateVersionId { get; set; }

    public int GroupNumber { get; set; }

    /// <summary>
    /// method_1 | method_2 | exempt
    /// </summary>
    public string? TaxMethod { get; set; }

    public int RulesetId { get; set; }

    /// <summary>
    /// active | archived
    /// </summary>
    public string Status { get; set; } = "active";

    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    // Navigation
    public virtual BusinessLocation BusinessLocation { get; set; } = null!;
    public virtual AccountingPeriod Period { get; set; } = null!;
    public virtual AccountingTemplateVersion TemplateVersion { get; set; } = null!;
    public virtual TaxRuleset Ruleset { get; set; } = null!;
    public virtual ICollection<AccountingBookBusinessType> BookBusinessTypes { get; set; } = new List<AccountingBookBusinessType>();
    public virtual ICollection<AccountingExport> Exports { get; set; } = new List<AccountingExport>();
    public virtual ICollection<FormulaResult> FormulaResults { get; set; } = new List<FormulaResult>();
}
