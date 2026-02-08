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

            try
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    UploadPreset = uploadPreset,
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    return new CloudinaryUploadResult
                    {
                        Success = false,
                        Error = uploadResult.Error.Message
                    };
                }

                return new CloudinaryUploadResult
                {
                    Success = true,
                    Url = uploadResult.SecureUrl.ToString(),
                    PublicId = uploadResult.PublicId
                };
            }
            catch (Exception ex)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
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

        public async Task<List<string>> GetAllPublicIdsAsync(string folder = "products")
        {
            var publicIds = new List<string>();

            try
            {
                var listParams = new ListResourcesParams
                {
                    Type = "upload",
                    MaxResults = 500
                };

                var listResult = await _cloudinary.ListResourcesAsync(listParams);

                foreach (var resource in listResult.Resources)
                {
                    publicIds.Add(resource.PublicId);
                }

                return publicIds;
            }
            catch
            {
                return publicIds;
            }
        }
    }
}
