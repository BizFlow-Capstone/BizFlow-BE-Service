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

        /// <summary>
        /// Get import with ProductsImports → Product and BusinessLocation
        /// </summary>
        Task<Import?> GetByIdWithItemsAsync(long importId);

        /// <summary>
        /// Get active import schema version
        /// </summary>
        Task<ImportSchemaVersion?> GetActiveSchemaVersionAsync();

        /// <summary>
        /// Count all imports (for import code generation)
        /// </summary>
        Task<int> CountAsync();

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
    }
}
