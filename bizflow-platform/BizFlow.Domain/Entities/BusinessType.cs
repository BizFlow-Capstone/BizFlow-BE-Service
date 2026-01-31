using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class BusinessType
{
    public Guid BusinessTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public Guid? CreatedById { get; set; }

    public Guid? ModifiedById { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime LastModifiedDate { get; set; }

    public virtual ICollection<BusinessTypeTax> BusinessTypeTaxes { get; set; } = new List<BusinessTypeTax>();

    public virtual User? CreatedBy { get; set; }

    public virtual User? ModifiedBy { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
