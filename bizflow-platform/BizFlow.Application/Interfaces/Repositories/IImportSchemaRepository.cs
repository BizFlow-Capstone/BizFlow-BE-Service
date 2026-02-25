using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IImportSchemaRepository
    {
        // ============ Query Methods ============

        /// <summary>
        /// Get all schemas (global query filter excludes soft-deleted)
        /// </summary>
        Task<List<ImportSchema>> GetAllAsync();

        /// <summary>
        /// Returns true if a schema with the given TemplateCode exists (optionally excluding a specific ID for updates)
        /// </summary>
        Task<bool> ExistsWithTemplateCodeAsync(string templateCode, int? excludeId = null);

        /// <summary>
        /// Get schema by ID (no navigation properties)
        /// </summary>
        Task<ImportSchema?> GetByIdAsync(int id);

        /// <summary>
        /// Get schema by ID with all versions loaded
        /// </summary>
        Task<ImportSchema?> GetByIdWithVersionsAsync(int id);

        /// <summary>
        /// Get the currently active schema with its active version
        /// </summary>
        Task<ImportSchema?> GetActiveAsync();

        // ============ Command Methods ============

        Task AddAsync(ImportSchema schema);

        void Update(ImportSchema schema);

        /// <summary>
        /// Hard-delete schema and its versions
        /// </summary>
        void Delete(ImportSchema schema);

        /// <summary>
        /// Set IsActive = false for ALL schemas (used before activating a specific one)
        /// </summary>
        Task DeactivateAllAsync();
    }
}
