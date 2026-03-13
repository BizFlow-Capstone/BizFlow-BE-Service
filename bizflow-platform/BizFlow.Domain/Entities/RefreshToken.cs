using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class RefreshToken
{
    public Guid RefreshTokenId { get; set; }

    /// <summary>
    /// FK to Accounts
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// SHA-256 hash of refresh token
    /// </summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>
    /// Random salt (Base64)
    /// </summary>
    public string TokenSalt { get; set; } = null!;

    /// <summary>
    /// User-Agent or device identifier
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Token expiry timestamp
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// NULL = active, NOT NULL = revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
