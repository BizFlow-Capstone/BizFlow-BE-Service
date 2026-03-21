using BizFlow.Application.Interfaces.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using BizFlow.Application.Common.Models;

namespace BizFlow.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _settings;

        public CloudinaryService(Cloudinary cloudinary, IOptions<CloudinarySettings> settings)
        {
            _cloudinary = cloudinary;
            _settings = settings.Value;
        }

        public async Task<CloudinaryUploadResult> UploadImageAsync(Stream fileStream, string fileName, string presetKey)
        {
            if (fileStream == null || fileStream.Length == 0)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    Error = "File is empty"
                };
            }

            if (!_settings.UploadPresets.TryGetValue(presetKey, out var uploadPreset))
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    Error = $"Upload preset not found for key: {presetKey}"
                };
            }

            // Retry logic
            int maxRetries = _settings.MaxRetries > 0 ? _settings.MaxRetries : 3;
            int attempt = 0;
            CloudinaryUploadResult? lastResult = null;

            while (attempt < maxRetries)
            {
                attempt++;
                try
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(fileName, fileStream),
                        UploadPreset = uploadPreset,
                        Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                    };

                    // Reset stream position if retrying
                    if (attempt > 1 && fileStream.CanSeek)
                    {
                        fileStream.Position = 0;
                    }

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.Error == null)
                    {
                        return new CloudinaryUploadResult
                        {
                            Success = true,
                            Url = uploadResult.SecureUrl.ToString(),
                            PublicId = uploadResult.PublicId
                        };
                    }
                    
                    lastResult = new CloudinaryUploadResult
                    {
                        Success = false,
                        Error = uploadResult.Error.Message
                    };
                }
                catch (Exception ex)
                {
                    lastResult = new CloudinaryUploadResult
                    {
                        Success = false,
                        Error = ex.Message
                    };
                }

                // Wait before retrying (exponential backoff: 500ms, 1000ms, 2000ms)
                if (attempt < maxRetries)
                {
                    await Task.Delay(500 * (int)Math.Pow(2, attempt - 1));
                }
            }

            return lastResult ?? new CloudinaryUploadResult { Success = false, Error = "Upload failed after multiple attempts" };
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return false;
            }

            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get a page of resources from Cloudinary with their upload timestamps.
        /// Uses cursor-based pagination to avoid loading everything into memory.
        /// </summary>
        /// <param name="prefix">Folder prefix to filter by (e.g. "bizflow_products/")</param>
        /// <param name="cursor">Pagination cursor from previous call (null for first page)</param>
        /// <param name="maxResults">Max results per page (default 100)</param>
        /// <returns>Page of (PublicId, CreatedAt) tuples and next cursor (null if no more pages)</returns>
        public async Task<CloudinaryResourcePage> GetResourcePageAsync(string prefix, string? cursor = null, int maxResults = 100)
        {
            try
            {
                var listParams = new ListResourcesByPrefixParams
                {
                    Type = "upload",
                    Prefix = prefix,
                    MaxResults = maxResults,
                    NextCursor = cursor
                };

                var result = await _cloudinary.ListResourcesAsync(listParams);

                var resources = result.Resources
                    .Select(r => new CloudinaryResourceInfo(
                        r.PublicId,
                        DateTime.TryParse(r.CreatedAt, out var dt) ? dt : DateTime.UtcNow))
                    .ToList();

                return new CloudinaryResourcePage
                {
                    Resources = resources,
                    NextCursor = result.NextCursor
                };
            }
            catch (Exception ex)
            {
                // Log the actual error so we know why the scan failed
                Console.Error.WriteLine($"[CloudinaryService] GetResourcePageAsync failed for prefix '{prefix}': {ex.Message}");
                return new CloudinaryResourcePage
                {
                    Resources = new List<CloudinaryResourceInfo>(),
                    NextCursor = null
                };
            }
        }
    }
}
