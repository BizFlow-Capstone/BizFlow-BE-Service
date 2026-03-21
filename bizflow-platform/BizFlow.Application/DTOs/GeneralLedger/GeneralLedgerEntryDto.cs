namespace BizFlow.Application.DTOs.GeneralLedger
{
    public class GeneralLedgerEntryDto
    {
        public long EntryId { get; set; }
        public int BusinessLocationId { get; set; }
        public string TransactionType { get; set; } = null!;
        public DateOnly EntryDate { get; set; }
        public string Description { get; set; } = null!;
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string? MoneyChannel { get; set; }
        public bool IsReversal { get; set; }
        public long? ReversedEntryId { get; set; }
        public DateTime CreatedAt { get; set; }

        public SourceLinkDto Source { get; set; } = null!;

        // = true if this entry is reversed, killed by another entry
        public bool IsReversed { get; set; }
        // id of the entry that reversed this entry
        public long? ReversalEntryId { get; set; }
        // number of entries that reversed this entry
        public int ReversalCount { get; set; }
        // reversal || reversed || active
        // reversal: this entry is a reversal entry
        // reversed: this entry is reversed by another entry
        // active: this entry is not reversed
        public string EffectiveStatus { get; set; } = null!;

        // Audit mode only: ordered from oldest ancestor to current entry.
        public List<long> HistoryChainEntryIds { get; set; } = new();
    }

    public class SourceLinkDto
    {
        public string ReferenceType { get; set; } = null!;
        public long? ReferenceId { get; set; }

        public string EntityType { get; set; } = null!;
        public long? EntityId { get; set; }
    }
}
