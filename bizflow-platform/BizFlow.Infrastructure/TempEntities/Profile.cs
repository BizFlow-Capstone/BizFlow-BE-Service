using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class Profiles
{
    public Guid ProfileId { get; set; }

    /// <summary>
    /// FK to Accounts
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Full name
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// Profile avatar URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Personal tax identification number
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Stripe Customer ID for payment orchestration
    /// </summary>
    public string? StripeCustomerId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Accounts Account { get; set; } = null!;

    public virtual ICollection<BusinessTypeTaxes> BusinessTypeTaxes { get; set; } = new List<BusinessTypeTaxes>();

    public virtual ICollection<BusinessTypes> BusinessTypesCreatedByNavigation { get; set; } = new List<BusinessTypes>();

    public virtual ICollection<BusinessTypes> BusinessTypesModifiedByNavigation { get; set; } = new List<BusinessTypes>();

    public virtual ICollection<DeviceTokens> DeviceTokens { get; set; } = new List<DeviceTokens>();

    public virtual ICollection<Hires> HiresEmployee { get; set; } = new List<Hires>();

    public virtual ICollection<Hires> HiresOwner { get; set; } = new List<Hires>();

    public virtual ICollection<ImportSchemaVersions> ImportSchemaVersions { get; set; } = new List<ImportSchemaVersions>();

    public virtual ICollection<Subscriptions> Subscriptions { get; set; } = new List<Subscriptions>();

    public virtual ICollection<SystemConfig> SystemConfig { get; set; } = new List<SystemConfig>();

    public virtual ICollection<Transactions> Transactions { get; set; } = new List<Transactions>();

    public virtual ICollection<UserLocationAssignments> UserLocationAssignments { get; set; } = new List<UserLocationAssignments>();
}
