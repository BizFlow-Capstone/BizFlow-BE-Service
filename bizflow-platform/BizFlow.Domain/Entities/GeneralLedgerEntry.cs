using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

/// <summary>
/// Immutable accounting ledger - append only, no updates/deletes
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
    /// Source entity ID (polymorphic, no hard FK)
    /// </summary>
    public long? ReferenceId { get; set; }

    /// <summary>
    /// Transaction date
    /// </summary>
    public DateOnly EntryDate { get; set; }

    /// <summary>
    /// Ledger entry description
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Debit amount
    /// </summary>
    public decimal DebitAmount { get; set; }

    /// <summary>
    /// Credit amount
    /// </summary>
    public decimal CreditAmount { get; set; }

    /// <summary>
    /// cash | bank | debt
    /// </summary>
    public string? MoneyChannel { get; set; }

    /// <summary>
    /// TRUE if this is a reversal entry
    /// </summary>
    public bool IsReversal { get; set; }

    /// <summary>
    /// Reversed entry ID (self-reference)
    /// </summary>
    public long? ReversedEntryId { get; set; }

    /// <summary>
    /// IMMUTABLE - must not be changed after creation
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual BusinessLocation BusinessLocation { get; set; } = null!;

    public virtual ICollection<GeneralLedgerEntry> InverseReversedEntry { get; set; } = new List<GeneralLedgerEntry>();

    public virtual GeneralLedgerEntry? ReversedEntry { get; set; }
}
