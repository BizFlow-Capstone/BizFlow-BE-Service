using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Debtor
{
    public class RecordDebtPaymentRequest
    {
        /// <summary>
        /// Signed amount: negative reduces debt, positive increases debt.
        /// Zero is not allowed and is validated in service.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>cash | bank</summary>
        [Required]
        public string PaymentMethod { get; set; } = null!;

        public string? Notes { get; set; }
    }
}
