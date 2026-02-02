using BizFlow.Application.Common.Models;

namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Extensible query parameters for filtering and searching products
    /// Inherits pagination from PaginationParams (defaults set via appsettings.json)
    /// Add new filter/search fields here as needed
    /// </summary>
    public class ProductQueryParams : PaginationParams
    {
        // Required
        public int LocationId { get; set; }

        // ============ SEARCH ============
        /// <summary>
        /// Search by product name (contains, case-insensitive)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Search by SKU (exact or contains)
        /// </summary>
        public string? Sku { get; set; }

        // ============ FILTER ============
        /// <summary>
        /// Filter by cost price range
        /// </summary>
        public decimal? MinCostPrice { get; set; }
        public decimal? MaxCostPrice { get; set; }

        /// <summary>
        /// Filter by stock quantity range
        /// </summary>
        public int? MinStock { get; set; }
        public int? MaxStock { get; set; }

        /// <summary>
        /// Filter by status (active, inactive, discontinued)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by track inventory flag
        /// </summary>
        public bool? TrackInventory { get; set; }

        // ============ ADD MORE FILTERS HERE ============
        // Example: public string? Manufacturer { get; set; }
        // Example: public Guid? BusinessTypeId { get; set; }
    }
}

