using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizFlow.Domain.Entities
{
    /// <summary>
    /// AI-generated anomaly alert. Written by AI Service (Python), read-only from .NET side.
    /// </summary>
    [Table("ai_anomaly_alerts")]
    public class AiAnomalyAlert
    {
        [Key]
        [Column("id")]
        [MaxLength(36)]
        public string Id { get; set; } = string.Empty;

        [Column("location_id")]
        [MaxLength(36)]
        public string LocationId { get; set; } = string.Empty;

        [Column("alert_type")]
        [MaxLength(100)]
        public string AlertType { get; set; } = string.Empty;

        [Column("severity")]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty;

        [Column("tier")]
        [MaxLength(20)]
        public string Tier { get; set; } = string.Empty;

        [Column("reference_date")]
        public DateTime ReferenceDate { get; set; }

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("reference_id")]
        [MaxLength(36)]
        public string? ReferenceId { get; set; }

        [Column("is_acknowledged")]
        public bool IsAcknowledged { get; set; }

        [Column("generated_at")]
        public DateTime GeneratedAt { get; set; }
    }
}
