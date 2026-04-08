namespace BizFlow.Application.Common.Models
{
    public class FirestoreHealthStatus
    {
        public bool IsConnected { get; set; }
        public string ProjectId { get; set; } = string.Empty;
        public string? ServiceAccountPath { get; set; }
        public bool ServiceAccountFileExists { get; set; }
        public string? Reason { get; set; }
    }
}
