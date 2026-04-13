using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Cost
{
    public class UpdateManualCostRequest
    {
        /// <summary>
        /// Cost type (see CostType enum). Cannot be 'import'.
        /// </summary>
        [MaxLength(30)]
        public string? CostType { get; set; }

        /// <summary>
        /// FK to BusinessTypes — related business sector (optional)
        /// </summary>
        public Guid? BusinessTypeId { get; set; }

        /// <summary>
        /// Cost description.
        /// </summary>
        [Required, MaxLength(500)]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Cost amount (positive).
        /// </summary>
        [Range(0.00, double.MaxValue)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Cost date (optional; defaults to today if not provided).
        /// </summary>
        public DateOnly? CostDate { get; set; }

        /// <summary>
        /// Payment method: cash | bank
        /// </summary>
        public string? PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? DocumentNumber { get; set; }

        public DateOnly? DocumentDate { get; set; }

        /// <summary>
        /// Set to true to remove the document image.
        /// </summary>
        public bool RemoveDocument { get; set; } = false;

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
