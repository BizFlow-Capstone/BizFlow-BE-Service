using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Import;
using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IImportRepository
    {
        // ============ Query Methods ============

        /// <summary>
        /// Get paginated imports with filters
        /// </summary>
        Task<(IEnumerable<Import> Items, int TotalCount)> SearchAsync(ImportQueryParams query);

        /// <summary>
        /// Get import by ID (no navigation)
        /// </summary>
        Task<Import?> GetByIdAsync(long importId);
        Task<List<Import>> GetByIdsAsync(IEnumerable<long> importIds);

        /// <summary>
        /// Get import with ProductsImports → Product and BusinessLocation
        /// </summary>
        Task<Import?> GetByIdWithItemsAsync(long importId);

        /// <summary>
        /// Count all imports (for import code generation)
        /// </summary>
        Task<int> CountAsync();

        /// <summary>
        /// Check which of the given PublicIds exist in the database (for orphan detection)
        /// </summary>
        Task<HashSet<string>> GetExistingPublicIdsAsync(IEnumerable<string> publicIds);

        // ============ Command Methods ============

        Task<Import> AddAsync(Import import);

        void Update(Import import);

        /// <summary>
        /// Hard-delete import
        /// </summary>
        void Delete(Import import);

        /// <summary>
        /// Remove a ProductImport item
        /// </summary>
        void DeleteItem(ProductImport item);

        /// <summary>
        /// Get a lookup of (ImportId, ProductId) → CostPrice for all imports at a location.
        /// Used by BookRenderingService and FormulaEngine to resolve import-time cost prices
        /// for stock movements in S2d rendering (don_gia, tien_nhap, tien_xuat, tien_ton).
        /// </summary>
        Task<Dictionary<(long ImportId, long ProductId), decimal>> GetImportCostLookupByLocationAsync(int locationId);

        // ── Replace-when-confirmed flow ──

        /// <summary>
        /// Must run inside a DB transaction. Acquires an InnoDB row-level write
        /// lock on the target Import row via <c>SELECT ... FOR UPDATE</c>.
        /// </summary>
        Task LockImportRowForUpdateAsync(long importId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the most recent replacement Import that references the given
        /// original ImportId via <c>RefImportId</c>.
        /// </summary>
        Task<Import?> GetLatestReplacementByRefImportIdAsync(long refImportId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Idempotent variant keyed by <c>IdempotencyKey</c>.
        /// </summary>
        Task<Import?> GetLatestReplacementByRefImportIdAsync(
            long refImportId,
            string idempotencyKey,
            CancellationToken cancellationToken = default);
    }
}
