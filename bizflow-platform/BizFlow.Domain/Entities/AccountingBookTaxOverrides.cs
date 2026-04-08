using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class AccountingBookTaxOverrides
{
    public long OverrideId { get; set; }

    public long BookId { get; set; }

    public Guid BusinessTypeId { get; set; }

    public decimal VatRate { get; set; }

    public decimal PitRate { get; set; }

    public string Note { get; set; } = null!;

    public Guid? UpdatedByUserId { get; set; }

    public DateTime UpdatedAt { get; set; }
}
