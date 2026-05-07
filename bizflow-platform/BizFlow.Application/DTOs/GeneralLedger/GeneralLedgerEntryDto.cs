using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.GeneralLedger
{
    public class GeneralLedgerEntryDto
    {
        public long EntryId { get; set; }
        public int BusinessLocationId { get; set; }
        /// <summary>
        /// Ledger transaction type. <c>Code</c> ∈ {<c>sale</c>, <c>import_cost</c>,
        /// <c>manual_cost</c>, <c>debt_payment</c>, <c>manual_revenue</c>, <c>manual_expense</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto TransactionType { get; set; } = null!;
        public DateOnly EntryDate { get; set; }
        public string Description { get; set; } = null!;
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        /// <summary>
        /// Money channel (optional). <c>Code</c> ∈ {<c>cash</c>, <c>bank</c>, <c>debt</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto? MoneyChannel { get; set; }
        public bool IsReversal { get; set; }
        public long? ReversedEntryId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Code { get; set; }

        public SourceLinkDto Source { get; set; } = null!;

        // = true if this entry is reversed, killed by another entry
        public bool IsReversed { get; set; }
        // id of the entry that reversed this entry
        public long? ReversalEntryId { get; set; }
        // number of entries that reversed this entry
        public int ReversalCount { get; set; }
        /// <summary>
        /// Effective status of this entry.
        /// <c>Code</c> ∈ {<c>active</c>, <c>reversed</c>, <c>reversal</c>}.
        /// <c>active</c> = not reversed, <c>reversed</c> = this entry has been reversed
        /// by another entry, <c>reversal</c> = this entry is itself a reversal entry.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto EffectiveStatus { get; set; } = null!;

        // Audit mode only: ordered from oldest ancestor to current entry.
        public List<long> HistoryChainEntryIds { get; set; } = new();
    }

    public class SourceLinkDto
    {
        /// <summary>
        /// Source reference type. <c>Code</c> ∈ {<c>cost</c>, <c>debtor_payment</c>, <c>revenue</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto ReferenceType { get; set; } = null!;
        public long? ReferenceId { get; set; }

        public string EntityType { get; set; } = null!;
        public long? EntityId { get; set; }
        public string? RootCode { get; set; }
    }
}
