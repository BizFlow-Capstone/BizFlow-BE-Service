namespace BizFlow.Application.DTOs.Import
{
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
}
