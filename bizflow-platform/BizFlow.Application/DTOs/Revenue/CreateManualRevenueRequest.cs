using System.ComponentModel.DataAnnotations;

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
    }
}
