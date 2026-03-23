using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class DeviceTokens
{
    /// <summary>
    /// UUID
    /// </summary>
    public Guid DeviceTokenId { get; set; }

    /// <summary>
    /// FK to Profiles
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// FCM Token
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// Device identifier
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// iOS, Android, Web
    /// </summary>
    public string Platform { get; set; } = null!;

    public DateTime RegisteredAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual Profiles Profile { get; set; } = null!;
}
