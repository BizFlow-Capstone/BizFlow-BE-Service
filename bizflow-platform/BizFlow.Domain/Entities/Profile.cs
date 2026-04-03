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

    /// <summary>
    /// Stripe Customer ID for payment orchestration
    /// </summary>
    public string? StripeCustomerId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<BusinessType> BusinessTypesCreatedByNavigation { get; set; } = new List<BusinessType>();

    public virtual ICollection<BusinessType> BusinessTypesModifiedByNavigation { get; set; } = new List<BusinessType>();

    public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();

    public virtual ICollection<Hire> HiresEmployee { get; set; } = new List<Hire>();

    public virtual ICollection<Hire> HiresOwner { get; set; } = new List<Hire>();

    public virtual ICollection<ImportSchemaVersion> ImportSchemaVersions { get; set; } = new List<ImportSchemaVersion>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public virtual ICollection<NotificationDispatch> NotificationDispatches { get; set; } = new List<NotificationDispatch>();

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();

    public virtual ICollection<SystemConfig> SystemConfig { get; set; } = new List<SystemConfig>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<UserLocationAssignment> UserLocationAssignments { get; set; } = new List<UserLocationAssignment>();
}
