using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BizFlow.Api")]

namespace BizFlow.Application.DTOs.Cost
{
    public class CreateManualCostRequest
    {
        [Required, Range(1, int.MaxValue)]
        public int BusinessLocationId { get; set; }

        /// <summary>
        /// FK to BusinessTypes — related business sector (optional)
        /// </summary>
        public Guid? BusinessTypeId { get; set; }

        /// <summary>
        /// Cost type (see CostType enum). Default: manual. Cannot be 'import'.
        /// </summary>
        [Required, MaxLength(30)]
        public string CostType { get; set; } = BizFlow.Domain.Enums.CostType.Manual;

        [Required, MaxLength(500)]
        public string Description { get; set; } = null!;

        [Range(0.00, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateOnly? CostDate { get; set; }

        /// <summary>Payment method: cash | bank</summary>
        public string? PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? DocumentNumber { get; set; }

        public DateOnly? DocumentDate { get; set; }

        /// <summary>
        /// Image stream (populated from IFormFile on controller).
        /// </summary>
        internal Stream? ImageStream { get; set; }

        /// <summary>
        /// Image file name (populated from IFormFile on controller).
        /// </summary>
        internal string? ImageFileName { get; set; }
    }
}
