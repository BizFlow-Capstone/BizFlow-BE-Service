using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class Account
{
    public Guid AccountId { get; set; }

    /// <summary>
    /// FK to Roles
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// BCrypt hash, NULL for Google-only accounts
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Account status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete timestamp
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Credential> Credentials { get; set; } = new List<Credential>();

    public virtual Profile? Profiles { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual Role Role { get; set; } = null!;
}
