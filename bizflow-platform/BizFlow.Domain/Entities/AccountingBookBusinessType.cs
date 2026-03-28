using System;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Bảng trung gian: 1 book gộp nhiều ngành cùng tax rate.
/// TaxProfileKey dạng readable: VAT_1.00|PIT_0.50|METHOD_method_1
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
