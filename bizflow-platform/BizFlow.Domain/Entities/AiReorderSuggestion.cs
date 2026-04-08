using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizFlow.Domain.Entities
{
    /// <summary>
    /// AI-computed reorder suggestion. Written by AI Service (Python), read-only from .NET side.
    /// </summary>
    [Table("ai_reorder_suggestions")]
    public class AiReorderSuggestion
    {
        [Key]
        [Column("id")]
        [MaxLength(36)]
        public string Id { get; set; } = string.Empty;

        [Column("location_id")]
        [MaxLength(36)]
        public string LocationId { get; set; } = string.Empty;

        [Column("product_id")]
        [MaxLength(36)]
        public string ProductId { get; set; } = string.Empty;

        [Column("current_stock")]
        public double CurrentStock { get; set; }

        [Column("days_until_stockout")]
        public int DaysUntilStockout { get; set; }

        [Column("suggested_quantity")]
        public double SuggestedQuantity { get; set; }

        [Column("avg_daily_sales")]
        public double AvgDailySales { get; set; }

        [Column("urgency")]
        [MaxLength(10)]
        public string Urgency { get; set; } = string.Empty;

        [Column("generated_at")]
        public DateTime GeneratedAt { get; set; }
    }
}
