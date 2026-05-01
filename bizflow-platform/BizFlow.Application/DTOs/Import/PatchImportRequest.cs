namespace BizFlow.Application.DTOs.Import
{
    public class PatchImportRequest
    {
        /// <summary>
        /// Required: date the goods were received
        /// </summary>
        public DateTime? ReceivedAt { get; set; }

        /// <summary>
        /// Optional voucher number to store on generated import cost.
        /// </summary>
        public string? DocumentNumber { get; set; }

        /// <summary>
        /// Optional voucher date to store on generated import cost.
        /// </summary>
        public DateOnly? DocumentDate { get; set; }

        /// <summary>
        /// Optional payment method to store on generated import cost: cash | bank.
        /// </summary>
        public string? PaymentMethod { get; set; }
    }
}
