using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Import
{
    public class CreateImportRequest
    {
        /// <summary>
        /// INVOICE | INVENTORY_ADJUSTMENT | RETURN
        /// </summary>
        public string ImportType { get; set; } = null!;

        [Required, Range(1, int.MaxValue)]
        public int BusinessLocationId { get; set; }

        public string? Supplier { get; set; }

        public string? Note { get; set; }

        /// <summary>
        /// Required when SaveAsDraft = false (immediate confirmation)
        /// </summary>
        public DateTime? ReceivedAt { get; set; }

        /// <summary>
        /// Voucher number written to Cost rows created from import (optional)
        /// </summary>
        public string? DocumentNumber { get; set; }

        /// <summary>
        /// Voucher date written to Cost rows created from import (optional)
        /// </summary>
        public DateOnly? DocumentDate { get; set; }

        /// <summary>
        /// If true → Status = DRAFT (stock NOT updated).
        /// If false (default) → Status = CONFIRMED immediately (stock updated).
        /// </summary>
        public bool SaveAsDraft { get; set; } = false;

        public List<ImportItemRequest> Items { get; set; } = new();

        internal Stream? ImageStream { get; set; }
        internal string? ImageFileName { get; set; }
    }
}
