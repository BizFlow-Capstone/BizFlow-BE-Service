using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class Role
{
    public Guid RoleId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime UpdateAt { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
