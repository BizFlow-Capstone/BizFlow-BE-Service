using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Sổ cái kế toán bất biến - chỉ thêm, không sửa/xoá
/// </summary>
public partial class GeneralLedgerEntry
{
    public long EntryId { get; set; }

    /// <summary>
    /// FK to BusinessLocations
    /// </summary>
    public int BusinessLocationId { get; set; }

    /// <summary>
    /// sale | import_cost | manual_cost | debt_payment | manual_revenue | manual_expense
    /// </summary>
    public string TransactionType { get; set; } = null!;

    /// <summary>
    /// order | cost | import | debtor_payment | revenue
    /// </summary>
    public string ReferenceType { get; set; } = null!;

    /// <summary>
    /// ID của thực thể nguồn (polymorphic, không có FK cứng)
    /// </summary>
    public long? ReferenceId { get; set; }

    /// <summary>
    /// Ngày phát sinh nghiệp vụ
    /// </summary>
    public DateOnly EntryDate { get; set; }

    /// <summary>
    /// Mô tả nội dung bút toán
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Số tiền Nợ (debit)
    /// </summary>
    public decimal DebitAmount { get; set; }

    /// <summary>
    /// Số tiền Có (credit)
    /// </summary>
    public decimal CreditAmount { get; set; }

    /// <summary>
    /// cash | bank | debt
    /// </summary>
    public string? MoneyChannel { get; set; }

    /// <summary>
    /// TRUE nếu đây là bản ghi đảo (reversal entry)
    /// </summary>
    public bool IsReversal { get; set; }

    /// <summary>
    /// EntryId bị đảo ngược (tự tham chiếu)
    /// </summary>
    public long? ReversedEntryId { get; set; }

    /// <summary>
    /// IMMUTABLE - không được thay đổi sau khi tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual ICollection<GeneralLedgerEntry> InverseReversedEntry { get; set; } = new List<GeneralLedgerEntry>();

    public virtual GeneralLedgerEntry? ReversedEntry { get; set; }
}
