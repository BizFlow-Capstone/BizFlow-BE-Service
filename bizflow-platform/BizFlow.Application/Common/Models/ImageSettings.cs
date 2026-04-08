namespace BizFlow.Application.Common.Models
{
    public class ImageSettings
    {
        public const string SectionName = "ImageSettings";

        /// <summary>
        /// Maximum file size in bytes (default: 5MB)
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

        /// <summary>
        /// Allowed image extensions (with dot, e.g. ".jpg")
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = new()
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };
    }
}
