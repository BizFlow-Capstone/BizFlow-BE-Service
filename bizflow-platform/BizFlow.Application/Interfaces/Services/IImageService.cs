using BizFlow.Application.Common.Models;

namespace BizFlow.Application.Interfaces.Services
{
    /// <summary>
    /// Shared service for image validation and upload.
    /// Deletion of orphan images is handled by ImageCleanupJob.
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Validate and upload an image. Throws BadRequestException on validation failure or upload error.
        /// </summary>
        /// <param name="fileStream">Image file stream</param>
        /// <param name="fileName">Original file name (used for extension validation)</param>
        /// <param name="folder">Cloudinary folder (e.g. "Products", "Imports")</param>
        /// <returns>Upload info with URL and PublicId</returns>
        Task<ImageUploadInfo> UploadImageAsync(Stream fileStream, string fileName, string folder);
    }
}
