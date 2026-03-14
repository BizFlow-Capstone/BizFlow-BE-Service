using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Manual stock adjustment request
    /// </summary>
    public class AdjustProductStockRequest
    {
        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [MaxLength(1000)]
        public string? Memo { get; set; }

        /// <summary>
        /// Optional override for import cost price when stock is increased.
        /// If omitted, current product cost price is used.
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? CostPrice { get; set; }
    }
}
