namespace BizFlow.Application.DTOs.GeneralLedger
{
    public class GeneralLedgerEntryDto
    {
        public long EntryId { get; set; }
        public int BusinessLocationId { get; set; }
        public string TransactionType { get; set; } = null!;
        public string ReferenceType { get; set; } = null!;
        public long? ReferenceId { get; set; }
        public DateOnly EntryDate { get; set; }
        public string Description { get; set; } = null!;
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string? MoneyChannel { get; set; }
        public bool IsReversal { get; set; }
        public long? ReversedEntryId { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsReversed { get; set; }
        public long? ReversalEntryId { get; set; }
        public int ReversalCount { get; set; }
        public string EffectiveStatus { get; set; } = null!;

        // Audit mode only: ordered from oldest ancestor to current entry.
        public List<long> HistoryChainEntryIds { get; set; } = new();
    }
}
