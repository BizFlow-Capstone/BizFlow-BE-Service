using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class BusinessLocation
{
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// Location/store name
    /// </summary>
    public string LocationName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? District { get; set; }

    public string? City { get; set; }

    public string? Phone { get; set; }

    /// <summary>
    /// Location email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// active, inactive
    /// </summary>
    public string Status { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete timestamp
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Business tax code
    /// </summary>
    public string? TaxCode { get; set; }

    public virtual ICollection<Import> Imports { get; set; } = new List<Import>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<UserLocationAssignment> UserLocationAssignments { get; set; } = new List<UserLocationAssignment>();
}
