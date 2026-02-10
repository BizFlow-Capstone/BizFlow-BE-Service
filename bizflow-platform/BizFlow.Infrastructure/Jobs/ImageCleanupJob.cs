using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    /// <summary>
    /// Hangfire job to cleanup orphaned images from Cloudinary
    /// </summary>
    public class ImageCleanupJob
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ImageCleanupJob> _logger;

        public ImageCleanupJob(
            ICloudinaryService cloudinaryService,
            IProductRepository productRepository,
            ILogger<ImageCleanupJob> logger)
        {
            _cloudinaryService = cloudinaryService;
            _productRepository = productRepository;
            _logger = logger;
        }

        /// <summary>
        /// Find and delete orphaned images from Cloudinary
        /// </summary>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting Cloudinary image cleanup job...");

            try
            {
                // Get all PublicIds from Cloudinary
                var cloudinaryPublicIds = await _cloudinaryService.GetAllPublicIdsAsync("products");
                _logger.LogInformation($"Found {cloudinaryPublicIds.Count} images in Cloudinary");

                // Get all PublicIds from database
                var dbPublicIds = await _productRepository.GetAllImagePublicIdsAsync();
                _logger.LogInformation($"Found {dbPublicIds.Count} PublicIds in database");

                // Find orphaned images (in Cloudinary but not in DB)
                var orphanedPublicIds = cloudinaryPublicIds.Except(dbPublicIds).ToList();
                _logger.LogInformation($"Found {orphanedPublicIds.Count} orphaned images");

                // Delete orphaned images
                int deletedCount = 0;
                foreach (var publicId in orphanedPublicIds)
                {
                    var deleted = await _cloudinaryService.DeleteImageAsync(publicId);
                    if (deleted)
                    {
                        deletedCount++;
                        _logger.LogInformation($"Deleted orphaned image: {publicId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to delete orphaned image: {publicId}");
                    }
                }

                _logger.LogInformation($"Cloudinary cleanup job completed. Deleted {deletedCount}/{orphanedPublicIds.Count} orphaned images");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Cloudinary cleanup job");
            }
        }
    }
}
