using BizFlow.Application.DTOs.ImportSchema;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IImportSchemaService
    {
        /// <summary>
        /// Get all import schemas — lightweight list (no version detail)
        /// </summary>
        Task<List<ImportSchemaListItemDto>> GetAllAsync();

        /// <summary>
        /// Get import schema by ID with its active version
        /// </summary>
        Task<ImportSchemaResponse> GetByIdAsync(int id);

        /// <summary>
        /// Create a new import schema with its first version
        /// </summary>
        Task<ImportSchemaResponse> CreateAsync(CreateImportSchemaRequest request);

        /// <summary>
        /// Update schema fields; creates a new version only if SchemaJson changed
        /// </summary>
        Task<ImportSchemaResponse> UpdateAsync(int id, UpdateImportSchemaRequest request);

        /// <summary>
        /// Activate a schema (deactivates all others first, sets EverActivated = true)
        /// </summary>
        Task ActivateAsync(int id);

        /// <summary>
        /// Delete schema: 400 if active, soft delete if ever activated, hard delete otherwise
        /// </summary>
        Task DeleteAsync(int id);
    }
}
