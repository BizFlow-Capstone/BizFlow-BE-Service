using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// System features available to subscription plans
/// </summary>
public partial class Feature
{
    public int FeatureId { get; set; }

    public string FeatureCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<FeatureUsage> FeatureUsages { get; set; } = new List<FeatureUsage>();

    public virtual ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
}
