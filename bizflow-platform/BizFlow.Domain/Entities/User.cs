namespace BizFlow.Domain.Entities;

public partial class User
{
    public Guid UserId { get; set; }

    /// <summary>
    /// User email (login)
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Hashed password
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Full name
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// Phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Profile avatar URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Personal tax identification number
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// User role
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Account status
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }

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

    public virtual ICollection<BusinessType> BusinessTypeCreatedBies { get; set; } = new List<BusinessType>();

    public virtual ICollection<BusinessType> BusinessTypeModifiedBies { get; set; } = new List<BusinessType>();

    public virtual ICollection<BusinessTypeTax> BusinessTypeTaxes { get; set; } = new List<BusinessTypeTax>();

    public virtual ICollection<Hire> HireEmployees { get; set; } = new List<Hire>();

    public virtual ICollection<Hire> HireOwners { get; set; } = new List<Hire>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<UserLocationAssignment> UserLocationAssignments { get; set; } = new List<UserLocationAssignment>();
}
