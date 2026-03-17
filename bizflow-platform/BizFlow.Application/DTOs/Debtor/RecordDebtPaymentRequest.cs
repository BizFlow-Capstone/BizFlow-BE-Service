using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Debtor
{
    public class RecordDebtPaymentRequest
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        /// <summary>cash | bank</summary>
        [Required]
        public string PaymentMethod { get; set; } = null!;

        public string? Notes { get; set; }
    }
}
