using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class Profile
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

    public DateTime UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<BusinessTypeTax> BusinessTypeTaxes { get; set; } = new List<BusinessTypeTax>();

    public virtual ICollection<BusinessType> BusinessTypesCreatedByNavigation { get; set; } = new List<BusinessType>();

    public virtual ICollection<BusinessType> BusinessTypesModifiedByNavigation { get; set; } = new List<BusinessType>();

    public virtual ICollection<Hire> HiresEmployee { get; set; } = new List<Hire>();

    public virtual ICollection<Hire> HiresOwner { get; set; } = new List<Hire>();

    public virtual ICollection<ImportSchemaVersion> ImportSchemaVersions { get; set; } = new List<ImportSchemaVersion>();

    public virtual ICollection<SystemConfig> SystemConfig { get; set; } = new List<SystemConfig>();
    public virtual ICollection<UserLocationAssignment> UserLocationAssignments { get; set; } = new List<UserLocationAssignment>();

    public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();
}
