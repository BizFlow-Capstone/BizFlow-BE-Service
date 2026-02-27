using BizFlow.Application.Common.Models;

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
        Task<CloudinaryUploadResult> UploadImageAsync(Stream fileStream, string fileName, string presetKey);

        /// <summary>
        /// Delete image from Cloudinary by PublicId
        /// </summary>
        Task<bool> DeleteImageAsync(string publicId);

        /// <summary>
        /// Get a page of resources from Cloudinary with timestamps (cursor-based pagination)
        /// </summary>
        Task<CloudinaryResourcePage> GetResourcePageAsync(string prefix, string? cursor = null, int maxResults = 100);
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
