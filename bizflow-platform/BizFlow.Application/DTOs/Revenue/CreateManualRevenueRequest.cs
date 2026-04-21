using System.ComponentModel.DataAnnotations;
using System.IO;

namespace BizFlow.Application.DTOs.Revenue
{
    public class CreateManualRevenueRequest
    {
        public int BusinessLocationId { get; set; }
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
