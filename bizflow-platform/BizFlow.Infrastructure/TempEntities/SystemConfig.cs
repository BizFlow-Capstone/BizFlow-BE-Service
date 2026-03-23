using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class SystemConfig
{
    public Guid SystemConfigId { get; set; }

    /// <summary>
    /// Configuration key name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Configuration value
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Description of this config entry
    /// </summary>
    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// FK to Profiles - who last updated
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    public virtual Profiles? UpdatedByNavigation { get; set; }
}
