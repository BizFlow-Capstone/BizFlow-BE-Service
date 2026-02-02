using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class BusinessLocation
{
    public int BusinessLocationId { get; set; }

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
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Business tax code
    /// </summary>
    public string? TaxCode { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<UserLocationAssignment> UserLocationAssignments { get; set; } = new List<UserLocationAssignment>();
}
