using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class MappableEntity
{
    public int EntityId { get; set; }

    public string EntityCode { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>
    /// revenue | cost | tax | cashflow | general
    /// </summary>
    public string Category { get; set; } = "revenue";

    public bool IsActive { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<MappableField> Fields { get; set; } = new List<MappableField>();
}
