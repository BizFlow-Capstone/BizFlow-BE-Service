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
    /// JSON: [1] hoặc [2,3,4]
    /// </summary>
    public string ApplicableGroups { get; set; } = null!;

    /// <summary>
    /// JSON: ["method_1"] hoặc NULL=tất cả
    /// </summary>
    public string? ApplicableMethods { get; set; }

    /// <summary>
    /// Data source for rendering: revenues | revenue_cost | gl_entries | stock_movements
    /// </summary>
    public string DataSourceType { get; set; } = "revenues";

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual ICollection<AccountingTemplateVersion> Versions { get; set; } = new List<AccountingTemplateVersion>();
}
