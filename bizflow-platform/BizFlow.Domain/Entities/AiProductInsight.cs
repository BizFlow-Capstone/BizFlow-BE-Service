using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizFlow.Domain.Entities
{
    /// <summary>
    /// AI-computed product insight. Written by AI Service (Python), read-only from .NET side.
    /// </summary>
    [Table("ai_product_insights")]
    public class AiProductInsight
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

        [Column("insight_type")]
        [MaxLength(50)]
        public string InsightType { get; set; } = string.Empty;

        [Column("rank")]
        public int Rank { get; set; }

        [Column("metric_value")]
        public double MetricValue { get; set; }

        [Column("period_days")]
        public int PeriodDays { get; set; }

        [Column("generated_at")]
        public DateTime GeneratedAt { get; set; }
    }
}
