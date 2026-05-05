using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.GeneralLedger
{
    public class GeneralLedgerTotalsQueryParams
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BusinessLocationId { get; set; }

        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
    }
}
