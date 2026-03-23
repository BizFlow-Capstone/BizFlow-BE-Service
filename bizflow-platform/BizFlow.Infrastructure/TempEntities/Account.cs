using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class Accounts
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

    public virtual ICollection<Credentials> Credentials { get; set; } = new List<Credentials>();

    public virtual Profiles? Profiles { get; set; }

    public virtual ICollection<RefreshTokens> RefreshTokens { get; set; } = new List<RefreshTokens>();

    public virtual Roles Role { get; set; } = null!;
}
