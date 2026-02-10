namespace BizFlow.Application.Interfaces.Services
{
    /// <summary>
    /// Service for managing images on Cloudinary
    /// </summary>
    public interface ICloudinaryService
    {
        /// <summary>
        /// Upload image to Cloudinary
        /// </summary>
        /// <param name="fileStream">Image file stream</param>
        /// <param name="fileName">Image file name</param>
        /// <param name="folder">Folder path in Cloudinary (default: "products")</param>
        /// <returns>Upload result with URL and PublicId</returns>
        Task<CloudinaryUploadResult> UploadImageAsync(Stream fileStream, string fileName, string presetKey);

        /// <summary>
        /// Delete image from Cloudinary by PublicId
        /// </summary>
        /// <param name="publicId">Cloudinary public ID</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteImageAsync(string publicId);

        /// <summary>
        /// Get all PublicIds from Cloudinary folder
        /// </summary>
        /// <param name="folder">Folder path (default: "products")</param>
        /// <returns>List of PublicIds</returns>
        Task<List<string>> GetAllPublicIdsAsync(string folder = "products");
    }

    /// <summary>
    /// Result of Cloudinary upload operation
    /// </summary>
    public class CloudinaryUploadResult
    {
        public bool Success { get; set; }
        public string? Url { get; set; }
        public string? PublicId { get; set; }
        public string? Error { get; set; }
    }
}
