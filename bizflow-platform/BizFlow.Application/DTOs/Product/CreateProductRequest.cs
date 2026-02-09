using System.ComponentModel.DataAnnotations;

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BizFlow.Api")]

namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Request to create a new product
    /// </summary>
    public class CreateProductRequest
    {
        [Required]
        public int LocationId { get; set; }

        [Required]
        public Guid BusinessTypeId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ProductName { get; set; } = null!;

        [MaxLength(100)]
        public string? Sku { get; set; }

        public bool TrackInventory { get; set; } = true;

        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        public int Stock { get; set; } = 0;

        internal Stream? ImageStream { get; set; }
        internal string? ImageFileName { get; set; }

        [MaxLength(255)]
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Price tiers for selling
        /// </summary>
        public List<PriceTierRequest> PriceTiers { get; set; } = new();
    }

    /// <summary>
    /// Price tier for product
    /// </summary>
    public class PriceTierRequest
    {
        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }
}
