using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class AccountingTemplate
{
    public int TemplateId { get; set; }

    public string TemplateCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>
    /// JSON: [1] or [2,3,4]
    /// </summary>
    public string ApplicableGroups { get; set; } = null!;

    /// <summary>
    /// JSON: ["method_1"] or NULL=all
    /// </summary>
    public string? ApplicableMethods { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual ICollection<AccountingTemplateVersion> Versions { get; set; } = new List<AccountingTemplateVersion>();
}
