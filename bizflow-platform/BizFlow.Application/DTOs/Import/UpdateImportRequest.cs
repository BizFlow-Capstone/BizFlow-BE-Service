namespace BizFlow.Application.DTOs.Import
{
    public class UpdateImportRequest
    {
        public string? ImportType { get; set; }

        public string? Supplier { get; set; }

        public string? Note { get; set; }

        public DateTime? ReceivedAt { get; set; }

        public List<ImportItemRequest>? Items { get; set; }

        /// <summary>
        /// Set to true to remove the current image without uploading a new one
        /// </summary>
        public bool RemoveImage { get; set; }

        internal Stream? ImageStream { get; set; }
        internal string? ImageFileName { get; set; }
    }
}
