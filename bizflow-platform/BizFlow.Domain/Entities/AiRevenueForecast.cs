using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizFlow.Domain.Entities
{
    /// <summary>
    /// AI-computed revenue forecast. Written by AI Service (Python), read-only from .NET side.
    /// </summary>
    [Table("ai_revenue_forecasts")]
    public class AiRevenueForecast
    {
        [Key]
        [Column("id")]
        [MaxLength(36)]
        public string Id { get; set; } = string.Empty;

        [Column("location_id")]
        [MaxLength(36)]
        public string LocationId { get; set; } = string.Empty;

        [Column("forecast_date")]
        [MaxLength(10)]
        public string ForecastDate { get; set; } = string.Empty;

        [Column("predicted_revenue")]
        public double PredictedRevenue { get; set; }

        [Column("lower_bound")]
        public double LowerBound { get; set; }

        [Column("upper_bound")]
        public double UpperBound { get; set; }

        [Column("trend_note")]
        public string? TrendNote { get; set; }

        [Column("generated_at")]
        public DateTime GeneratedAt { get; set; }
    }
}
