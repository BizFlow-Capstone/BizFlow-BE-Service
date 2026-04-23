using System.ComponentModel.DataAnnotations;
using System.IO;

namespace BizFlow.Application.DTOs.Revenue
{
    public class UpdateManualRevenueRequest
    {
        [Required]
        public Guid? BusinessTypeId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateOnly? RevenueDate { get; set; }

        [Required, MaxLength(500)]
        public string Description { get; set; } = null!;

        /// <summary>
        /// cash | bank
        /// </summary>
        public string MoneyChannel { get; set; } = null!;

        [MaxLength(100)]
        public string? DocumentNumber { get; set; }

        public DateOnly? DocumentDate { get; set; }

        public bool RemoveDocument { get; set; }

        /// <summary>
        /// Client-supplied idempotency key for the replace-when-posted flow.
        /// Lets the client retry the same edit safely and receive the same replacement record.
        /// Ignored when the Revenue is still in <c>draft</c> status (in-place update applies).
        /// </summary>
        [MaxLength(100)]
        public string? IdempotencyKey { get; set; }

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
