using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class BusinessType
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

    public virtual ICollection<BusinessTypeTax> BusinessTypeTaxes { get; set; } = new List<BusinessTypeTax>();

    public virtual Profile? CreatedByNavigation { get; set; }

    public virtual Profile? ModifiedByNavigation { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
