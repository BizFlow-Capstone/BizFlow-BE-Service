namespace BizFlow.Application.DTOs.Import
{
    public class UpdateImportRequest
    {
        public string? ImportType { get; set; }

        public string? Supplier { get; set; }

        public string? Note { get; set; }

        public DateTime? ReceivedAt { get; set; }

        public List<ImportItemRequest>? Items { get; set; }
    }
}
