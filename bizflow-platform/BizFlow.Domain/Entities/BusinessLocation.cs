namespace BizFlow.Domain.Entities;

public partial class BusinessLocation
{
    public Guid BusinessLocationId { get; set; }

    /// <summary>
    /// Location/store name
    /// </summary>
    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? District { get; set; }

    public string? City { get; set; }

    public string? Phone { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// Tax identification number
    /// </summary>
    public string? TaxCode { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<UserLocationAssignment> UserLocationAssignments { get; set; } = new List<UserLocationAssignment>();
}
