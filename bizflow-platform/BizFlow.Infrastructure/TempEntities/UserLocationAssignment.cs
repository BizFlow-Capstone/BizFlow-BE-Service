using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class UserLocationAssignments
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

    public virtual BusinessLocations BusinessLocation { get; set; } = null!;

    public virtual Profiles User { get; set; } = null!;
}
