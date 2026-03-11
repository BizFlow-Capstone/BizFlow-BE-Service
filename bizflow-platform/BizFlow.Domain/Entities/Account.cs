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
    /// User email (login)
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Hashed password
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Account status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Email verification status
    /// </summary>
    public bool EmailVerified { get; set; }

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

    public virtual Profile? Profile { get; set; }

    public virtual Role Role { get; set; } = null!;
}
