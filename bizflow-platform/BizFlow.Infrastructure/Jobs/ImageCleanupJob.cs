using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Jobs
{
    /// <summary>
    /// Hangfire job to cleanup orphaned images from Cloudinary.
    /// Scans all Cloudinary resources page by page, deletes those not referenced in DB.
    /// NOTE: Upload presets use Cloudinary virtual folders — public_id does NOT include folder path.
    /// </summary>
    public class ImageCleanupJob
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IProductRepository _productRepository;
        private readonly IImportRepository _importRepository;
        private readonly IRevenueRepository _revenueRepository;
        private readonly ICostRepository _costRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly ILogger<ImageCleanupJob> _logger;

        /// <summary>
        /// Grace period — only delete images uploaded more than this duration ago
        /// to avoid race conditions with in-progress uploads
        /// </summary>
        private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(10);

        public ImageCleanupJob(
            ICloudinaryService cloudinaryService,
            IProductRepository productRepository,
            IImportRepository importRepository,
            IRevenueRepository revenueRepository,
            ICostRepository costRepository,
            IProfileRepository profileRepository,
            ILogger<ImageCleanupJob> logger)
        {
            _cloudinaryService = cloudinaryService;
            _productRepository = productRepository;
            _importRepository = importRepository;
            _revenueRepository = revenueRepository;
            _costRepository = costRepository;
            _profileRepository = profileRepository;
            _logger = logger;
        }

        /// <summary>
        /// Find and delete orphaned images from Cloudinary, processing page by page
        /// </summary>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting Cloudinary image cleanup job...");

            var cutoff = DateTime.UtcNow - GracePeriod;
            int totalDeleted = 0;
            int totalFailed = 0;
            int totalScanned = 0;
            string? cursor = null;

            try
            {
                do
                {
                    // 1. Get one page of all Cloudinary resources (no prefix — virtual folders)
                    var page = await _cloudinaryService.GetResourcePageAsync("", cursor);
                    cursor = page.NextCursor;

                    if (page.Resources.Count == 0) break;
                    totalScanned += page.Resources.Count;

                    // 2. Filter by grace period — skip recently uploaded images
                    var candidates = page.Resources
                        .Where(r => r.CreatedAt < cutoff)
                        .Select(r => r.PublicId)
                        .ToList();

                    if (candidates.Count == 0) continue;

                    // 3. Batch check against DB — which PublicIds are still referenced?
                    var existsInProducts = await _productRepository.GetExistingPublicIdsAsync(candidates);
                    var existsInImports = await _importRepository.GetExistingPublicIdsAsync(candidates);
                    var existsInRevenues = await _revenueRepository.GetExistingPublicIdsAsync(candidates);
                    var existsInCosts = await _costRepository.GetExistingPublicIdsAsync(candidates);
                    var existsInProfiles = await _profileRepository.GetExistingAvatarPublicIdsAsync(candidates);

                    var orphans = candidates
                        .Where(id =>
                            !existsInProducts.Contains(id)
                            && !existsInImports.Contains(id)
                            && !existsInRevenues.Contains(id)
                            && !existsInCosts.Contains(id)
                            && !existsInProfiles.Contains(id))
                        .ToList();

                    _logger.LogInformation(
                        "Page: {Total} scanned, {Candidates} checked, refs Product={ProductRefs}, Import={ImportRefs}, Revenue={RevenueRefs}, Cost={CostRefs}, Profile={ProfileRefs}, {Orphans} orphans found",
                        page.Resources.Count,
                        candidates.Count,
                        existsInProducts.Count,
                        existsInImports.Count,
                        existsInRevenues.Count,
                        existsInCosts.Count,
                        existsInProfiles.Count,
                        orphans.Count);

                    if (orphans.Count == 0) continue;

                    // 4. Delete orphans concurrently (batch of 10)
                    foreach (var batch in orphans.Chunk(10))
                    {
                        var tasks = batch.Select(async publicId =>
                        {
                            var success = await _cloudinaryService.DeleteImageAsync(publicId);
                            return (publicId, success);
                        });

                        var results = await Task.WhenAll(tasks);

                        foreach (var (publicId, success) in results)
                        {
                            if (success)
                            {
                                totalDeleted++;
                                _logger.LogDebug("Deleted orphaned image: {PublicId}", publicId);
                            }
                            else
                            {
                                totalFailed++;
                                _logger.LogWarning("Failed to delete orphaned image: {PublicId}", publicId);
                            }
                        }
                    }

                } while (cursor != null);

                _logger.LogInformation(
                    "Cloudinary cleanup completed. Scanned: {Scanned}, Deleted: {Deleted}, Failed: {Failed}",
                    totalScanned, totalDeleted, totalFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Cloudinary cleanup job");
            }
        }
    }
}
