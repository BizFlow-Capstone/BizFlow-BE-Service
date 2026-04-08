using System;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Bridge table: one book can map multiple business types with the same tax rate.
/// TaxProfileKey readable format: VAT_1.00|PIT_0.50|METHOD_method_1
/// </summary>
public partial class AccountingBookBusinessType
{
    public long Id { get; set; }
    public long BookId { get; set; }
    public Guid BusinessTypeId { get; set; }

    /// <summary>
    /// Readable key: VAT_1.00|PIT_0.50|METHOD_method_1
    /// For debug/trace — explains why business types are grouped together.
    /// </summary>
    public string TaxProfileKey { get; set; } = null!;

    // Navigation
    public virtual AccountingBook Book { get; set; } = null!;
    public virtual BusinessType BusinessType { get; set; } = null!;
}
