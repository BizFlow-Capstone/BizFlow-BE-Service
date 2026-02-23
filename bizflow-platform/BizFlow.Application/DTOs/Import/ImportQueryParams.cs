using BizFlow.Application.Common.Models;

namespace BizFlow.Application.DTOs.Import
{
    /// <summary>
    /// Query parameters for listing/filtering imports with pagination
    /// </summary>
    public class ImportQueryParams : PaginationParams
    {
        /// <summary>
        /// Filter by status: DRAFT | CONFIRMED | CANCELLED
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by import type: INVOICE | INVENTORY_ADJUSTMENT | RETURN
        /// </summary>
        public string? ImportType { get; set; }

        /// <summary>
        /// Filter by business location
        /// </summary>
        public int? BusinessLocationId { get; set; }

        /// <summary>
        /// Filter imports from this date (inclusive)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter imports to this date (inclusive)
        /// </summary>
        public DateTime? ToDate { get; set; }
    }
}
