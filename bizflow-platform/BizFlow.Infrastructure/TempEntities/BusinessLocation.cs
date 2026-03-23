using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class BusinessLocations
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

    public virtual ICollection<Costs> Costs { get; set; } = new List<Costs>();

    public virtual ICollection<Debtors> Debtors { get; set; } = new List<Debtors>();

    public virtual ICollection<GeneralLedgerEntries> GeneralLedgerEntries { get; set; } = new List<GeneralLedgerEntries>();

    public virtual ICollection<Imports> Imports { get; set; } = new List<Imports>();

    public virtual ICollection<Products> Products { get; set; } = new List<Products>();

    public virtual ICollection<Revenues> Revenues { get; set; } = new List<Revenues>();

    public virtual ICollection<UserLocationAssignments> UserLocationAssignments { get; set; } = new List<UserLocationAssignments>();
}
