using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class AccountingTemplateVersion
{
    public int TemplateVersionId { get; set; }
    public int TemplateId { get; set; }

    public string VersionLabel { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public string? TemplateFileUrl { get; set; }
    public string? ChangeNotes { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual AccountingTemplate Template { get; set; } = null!;
    public virtual ICollection<TemplateFieldMapping> FieldMappings { get; set; } = new List<TemplateFieldMapping>();
    public virtual ICollection<TemplateRowDefinition> RowDefinitions { get; set; } = new List<TemplateRowDefinition>();
    public virtual ICollection<AccountingBook> AccountingBooks { get; set; } = new List<AccountingBook>();
}
