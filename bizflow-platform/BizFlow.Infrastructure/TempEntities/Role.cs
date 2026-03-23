using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class Roles
{
    public Guid RoleId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime UpdateAt { get; set; }

    public virtual ICollection<Accounts> Accounts { get; set; } = new List<Accounts>();
}
