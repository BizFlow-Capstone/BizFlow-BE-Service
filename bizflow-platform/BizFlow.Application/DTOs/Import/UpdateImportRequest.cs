using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Import
{
    public class UpdateImportRequest
    {
        public string? ImportType { get; set; }

        public string? Supplier { get; set; }

        public string? Note { get; set; }

        public DateTime? ReceivedAt { get; set; }

        public List<ImportItemRequest>? Items { get; set; }

        /// <summary>
        /// Voucher number written to Cost rows created from this import (optional).
        /// Used when replacing a CONFIRMED import (new Cost row).
        /// </summary>
        [MaxLength(100)]
        public string? DocumentNumber { get; set; }

        /// <summary>
        /// Voucher date for the Cost row (optional).
        /// </summary>
        public DateOnly? DocumentDate { get; set; }

        /// <summary>
        /// Payment method for the Cost row created from import (optional): cash | bank.
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Required when editing a CONFIRMED import (replace-when-confirmed flow).
        /// Ignored for DRAFT updates.
        /// </summary>
        [MaxLength(100)]
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// Set to true to remove the current image without uploading a new one
        /// </summary>
        public bool RemoveImage { get; set; }

        internal Stream? ImageStream { get; set; }
        internal string? ImageFileName { get; set; }
    }
}
