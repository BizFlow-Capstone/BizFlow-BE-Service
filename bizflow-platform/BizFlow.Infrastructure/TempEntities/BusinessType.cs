using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class BusinessTypes
{
    public Guid BusinessTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// active, inactive
    /// </summary>
    public string Status { get; set; } = null!;

    public Guid? CreatedBy { get; set; }

    public Guid? ModifiedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public virtual ICollection<BusinessTypeTaxes> BusinessTypeTaxes { get; set; } = new List<BusinessTypeTaxes>();

    public virtual Profiles? CreatedByNavigation { get; set; }

    public virtual Profiles? ModifiedByNavigation { get; set; }

    public virtual ICollection<Products> Products { get; set; } = new List<Products>();
}
