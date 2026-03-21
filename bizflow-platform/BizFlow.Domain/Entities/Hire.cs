using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class Hire
{
    public int HireId { get; set; }

    /// <summary>
    /// Owner who hired the employee
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Employee being hired
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Hiring status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// pending, accepted, rejected
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Invitation timestamp
    /// </summary>
    public DateTime InvitedAt { get; set; }

    /// <summary>
    /// Start date of employment (NULL when pending/rejected)
    /// </summary>
    public DateTime? StartAt { get; set; }

    /// <summary>
    /// End date of employment (NULL if still active)
    /// </summary>
    public DateTime? EndAt { get; set; }

    public virtual Profile Employee { get; set; } = null!;

    public virtual Profile Owner { get; set; } = null!;
}
