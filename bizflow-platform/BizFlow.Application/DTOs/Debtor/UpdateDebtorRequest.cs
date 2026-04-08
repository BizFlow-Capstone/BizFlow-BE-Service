using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Debtor
{
    public class UpdateDebtorRequest
    {
        [Required, MaxLength(255)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public string? Address { get; set; }
        public string? Notes { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CreditLimit { get; set; }
    }
}
