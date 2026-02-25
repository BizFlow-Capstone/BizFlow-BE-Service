namespace BizFlow.Application.DTOs.Import
{
    /// <summary>
    /// Response for PATCH confirm/cancel
    /// </summary>
    public class ImportPatchResultDto
    {
        public long ImportId { get; set; }
        public string? ImportCode { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ReceivedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
