using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

/// <summary>
/// System features available to subscription plans
/// </summary>
public partial class Features
{
    public int FeatureId { get; set; }

    public string FeatureCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<FeatureUsages> FeatureUsages { get; set; } = new List<FeatureUsages>();

    public virtual ICollection<PlanFeatures> PlanFeatures { get; set; } = new List<PlanFeatures>();
}
