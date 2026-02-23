using BizFlow.Application.Common.Models;

namespace BizFlow.Application.DTOs.Import
{
    // =========================================================
    // REQUEST DTOs
    // =========================================================

    public class ImportItemRequest
    {
        public long ProductId { get; set; }

        /// <summary>
        /// Quantity in base unit
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Cost price per base unit
        /// </summary>
        public decimal CostPrice { get; set; }
    }

    public class CreateImportRequest
    {
        /// <summary>
        /// INVOICE | INVENTORY_ADJUSTMENT | RETURN
        /// </summary>
        public string ImportType { get; set; } = null!;

        public int BusinessLocationId { get; set; }

        public string? Supplier { get; set; }

        public string? Note { get; set; }

        /// <summary>
        /// Required when SaveAsDraft = false (immediate confirmation)
        /// </summary>
        public DateTime? ReceivedAt { get; set; }

        /// <summary>
        /// If true → Status = DRAFT (stock NOT updated).
        /// If false (default) → Status = CONFIRMED immediately (stock updated).
        /// </summary>
        public bool SaveAsDraft { get; set; } = false;

        public List<ImportItemRequest> Items { get; set; } = new();
    }

    public class UpdateImportRequest
    {
        public string? ImportType { get; set; }

        public string? Supplier { get; set; }

        public string? Note { get; set; }

        public DateTime? ReceivedAt { get; set; }

        public List<ImportItemRequest>? Items { get; set; }
    }

    public class PatchImportRequest
    {
        /// <summary>
        /// Required: date the goods were received
        /// </summary>
        public DateTime? ReceivedAt { get; set; }
    }

    // =========================================================
    // RESPONSE DTOs
    // =========================================================

    /// <summary>
    /// Used in list and create/update responses
    /// </summary>
    public class ImportSummaryDto
    {
        public long ImportId { get; set; }
        public string? ImportCode { get; set; }
        public string ImportType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int BusinessLocationId { get; set; }
        public string? BusinessLocationName { get; set; }
        public string? Supplier { get; set; }
        public string? Note { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Full detail response including items
    /// </summary>
    public class ImportDetailDto : ImportSummaryDto
    {
        public string? ImageUrl { get; set; }
        public List<ImportItemDetailDto> Items { get; set; } = new();
    }

    public class ImportItemDetailDto
    {
        public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public string? BaseUnit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int? CurrentStock { get; set; }
    }

    /// <summary>
    /// Response for PATCH confirm/cancel
    /// </summary>
    public class ImportPatchResultDto
    {
        public long ImportId { get; set; }
        public string? ImportCode { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ReceivedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Returns the JSON schema of the active ImportSchemaVersion
    /// </summary>
    public class ImportSchemaDto
    {
        public string SchemaJson { get; set; } = null!;
    }
}
