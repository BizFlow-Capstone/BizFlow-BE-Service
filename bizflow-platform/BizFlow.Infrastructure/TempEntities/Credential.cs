using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class Credentials
{
    public Guid CredentialId { get; set; }

    /// <summary>
    /// FK to Accounts
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Credential type
    /// </summary>
    public string Type { get; set; } = null!;

    /// <summary>
    /// Phone: +84xxx, Email: user@mail, Google: sub-id
    /// </summary>
    public string Identifier { get; set; } = null!;

    /// <summary>
    /// Only used for type=email
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Only used for type=google (informational)
    /// </summary>
    public string? GoogleEmail { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Accounts Account { get; set; } = null!;
}
