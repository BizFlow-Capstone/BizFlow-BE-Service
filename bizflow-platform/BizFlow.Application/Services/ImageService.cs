using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Models;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace BizFlow.Application.Services
{
    /// <summary>
    /// Shared image service: validates file type/size, uploads to Cloudinary, handles replace &amp; delete
    /// </summary>
    public class ImageService : IImageService
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ImageSettings _settings;

        /// <summary>
        /// Magic byte signatures for common image formats
        /// </summary>
        private static readonly Dictionary<string, byte[][]> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg",  new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png",  new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { ".gif",  new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } } // RIFF header
        };

        public ImageService(ICloudinaryService cloudinaryService, IOptions<ImageSettings> settings)
        {
            _cloudinaryService = cloudinaryService;
            _settings = settings.Value;

            // Startup validation: ensure every allowed extension has a magic bytes signature defined
            var unsupported = _settings.AllowedExtensions
                .Where(ext => !MagicBytes.ContainsKey(ext))
                .ToList();

            if (unsupported.Any())
            {
                throw new InvalidOperationException(
                    $"ImageSettings.AllowedExtensions contains extensions without magic bytes signatures: " +
                    $"{string.Join(", ", unsupported)}. " +
                    $"Please add magic bytes for these extensions in ImageService.MagicBytes, " +
                    $"or remove them from AllowedExtensions in appsettings.json.");
            }
        }

        public async Task<ImageUploadInfo> UploadImageAsync(Stream fileStream, string? fileName, ImageUploadTarget target)
        {
            var safeFileName = string.IsNullOrWhiteSpace(fileName)
                ? ImageUploadConstants.DefaultFileName
                : fileName;

            var presetKey = ImageUploadConstants.GetPresetKey(target);

            ValidateImage(fileStream, safeFileName);

            var uploadResult = await _cloudinaryService.UploadImageAsync(fileStream, safeFileName, presetKey);

            if (!uploadResult.Success)
            {
                throw new BadRequestException(MessageKeys.ImageUploadFailed, null, uploadResult.Error ?? "Unknown error");
            }

            return new ImageUploadInfo(uploadResult.Url!, uploadResult.PublicId!);
        }



        // =========================================================
        // Validation
        // =========================================================

        private void ValidateImage(Stream fileStream, string fileName)
        {
            // 1. Check file size
            if (fileStream.Length > _settings.MaxFileSizeBytes)
            {
                var maxSizeMB = _settings.MaxFileSizeBytes / (1024 * 1024);
                throw new BadRequestException(MessageKeys.ImageFileTooLarge, null, $"{maxSizeMB}MB");
            }

            // 2. Check extension
            var extension = Path.GetExtension(fileName);
            var allowedSet = new HashSet<string>(_settings.AllowedExtensions, StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(extension) || !allowedSet.Contains(extension))
            {
                var allowed = string.Join(", ", _settings.AllowedExtensions);
                throw new BadRequestException(MessageKeys.ImageInvalidFileType, null, allowed);
            }

            // 3. Check magic bytes (verify file content matches extension)
            if (MagicBytes.TryGetValue(extension, out var signatures))
            {
                var headerBuffer = new byte[8];
                var originalPosition = fileStream.Position;
                fileStream.Position = 0;
                var bytesRead = fileStream.Read(headerBuffer, 0, headerBuffer.Length);
                fileStream.Position = originalPosition;

                if (bytesRead < 3)
                {
                    throw new BadRequestException(MessageKeys.ImageInvalidFileType, null, string.Join(", ", _settings.AllowedExtensions));
                }

                var isValid = signatures.Any(sig =>
                    sig.Length <= bytesRead &&
                    headerBuffer.Take(sig.Length).SequenceEqual(sig));

                if (!isValid)
                {
                    throw new BadRequestException(MessageKeys.ImageInvalidFileType, null, string.Join(", ", _settings.AllowedExtensions));
                }
            }
        }
    }
}
