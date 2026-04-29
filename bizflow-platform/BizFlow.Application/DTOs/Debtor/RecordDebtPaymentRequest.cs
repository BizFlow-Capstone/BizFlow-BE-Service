using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Debtor
{
    public class RecordDebtPaymentRequest
    {
        /// <summary>
        /// Absolute amount (must be greater than zero).
        /// Debt impact is determined by <see cref="Action"/>.
        /// </summary>
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; set; }

        /// <summary>
        /// decrease_debt | increase_debt
        /// </summary>
        [Required]
        public string Action { get; set; } = null!;

        /// <summary>cash | bank</summary>
        [Required]
        public string PaymentMethod { get; set; } = null!;

        public string? Notes { get; set; }
    }
}
