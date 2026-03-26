using System;

namespace BizFlow.Domain.Entities;

public partial class MappableField
{
    public int FieldId { get; set; }
    public int EntityId { get; set; }

    public string FieldCode { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>
    /// decimal | date | text | integer | boolean
    /// </summary>
    public string DataType { get; set; } = null!;

    /// <summary>
    /// JSON: ["sum","avg","none"]
    /// </summary>
    public string AllowedAggregations { get; set; } = null!;

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual MappableEntity Entity { get; set; } = null!;
}
