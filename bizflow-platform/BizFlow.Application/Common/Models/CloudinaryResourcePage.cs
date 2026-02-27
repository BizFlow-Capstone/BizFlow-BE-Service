namespace BizFlow.Application.Common.Models
{
    /// <summary>
    /// A single Cloudinary resource with metadata for cleanup
    /// </summary>
    public record CloudinaryResourceInfo(string PublicId, DateTime CreatedAt);

    /// <summary>
    /// A page of Cloudinary resources with pagination cursor
    /// </summary>
    public class CloudinaryResourcePage
    {
        public List<CloudinaryResourceInfo> Resources { get; set; } = new();

        /// <summary>
        /// Cursor for next page. Null if no more pages.
        /// </summary>
        public string? NextCursor { get; set; }
    }
}
