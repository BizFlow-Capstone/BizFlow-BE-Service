using System.ComponentModel.DataAnnotations;
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
        [Required]
        [Range(1, int.MaxValue)]
        public int LocationId { get; set; }

        // ============ SEARCH ============
        /// <summary>
        /// Unified search keyword — matches product name OR SKU (contains, case-insensitive).
        /// Use this for a single search box on FE.
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Filter by product name only (contains, case-insensitive)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Filter by SKU only (contains)
        /// </summary>
        public string? Sku { get; set; }

        // ============ FILTER ============
        /// <summary>
        /// Filter by selling price range
        /// </summary>
        public decimal? MinSellingPrice { get; set; }
        public decimal? MaxSellingPrice { get; set; }

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
         public Guid? BusinessTypeId { get; set; }
    }
}

