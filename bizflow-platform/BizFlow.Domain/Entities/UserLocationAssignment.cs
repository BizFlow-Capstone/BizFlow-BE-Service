using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class UserLocationAssignment
{
    public int UserLocationAssignmentId { get; set; }

    /// <summary>
    /// Assigned user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Assigned location
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Is the owner of this location
    /// </summary>
    public bool IsOwner { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// When employee/user was assigned to location
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// When employee/user was removed from location
    /// </summary>
    public DateTime? UnassignedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual Profile User { get; set; } = null!;
}
