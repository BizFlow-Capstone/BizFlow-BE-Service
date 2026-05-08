using BizFlow.Application.Common.Models;
using BizFlow.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.GeneralLedger
{
    public class GeneralLedgerQueryParams : PaginationParams
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BusinessLocationId { get; set; }

        public List<string>? TransactionTypes { get; set; }
        public List<string>? ReferenceTypes { get; set; }
        public List<string>? MoneyChannels { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string ViewMode { get; set; } = GeneralLedgerViewMode.Audit;

        /// <summary>
        /// When true, reversal entries (IsReversal == true) are excluded at the DB level.
        /// Use this for accounting book queries so that hasMore / cursor logic is correct.
        /// </summary>
        public bool ExcludeReversal { get; set; }

        /// <summary>
        /// When true, entries with null MoneyChannel are excluded at the DB level.
        /// Use this for accounting book queries to hide unclassified cost/import entries.
        /// </summary>
        public bool ExcludeNullMoneyChannel { get; set; }
    }
}
