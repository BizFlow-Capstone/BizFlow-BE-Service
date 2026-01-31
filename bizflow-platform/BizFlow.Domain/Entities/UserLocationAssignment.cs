namespace BizFlow.Domain.Entities;

public partial class UserLocationAssignment
{
    public Guid UserLocationAssignmentId { get; set; }

    /// <summary>
    /// Assigned user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Assigned location
    /// </summary>
    public Guid BusinessLocationId { get; set; }

    /// <summary>
    /// Is the owner of this location
    /// </summary>
    public bool IsOwner { get; set; }

    public bool? IsActive { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
