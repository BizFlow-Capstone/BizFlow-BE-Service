using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class ImportSchemaRepository : IImportSchemaRepository
    {
        private readonly BizFlowDbContext _dbContext;

        public ImportSchemaRepository(BizFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ============ Query Methods ============

        public async Task<List<ImportSchema>> GetAllAsync()
        {
            return await _dbContext.ImportSchemas
                .OrderByDescending(s => s.ImportSchemaId)
                .ToListAsync();
        }

        public async Task<bool> ExistsWithTemplateCodeAsync(string templateCode, int? excludeId = null)
        {
            return await _dbContext.ImportSchemas
                .AnyAsync(s => s.TemplateCode == templateCode && (excludeId == null || s.ImportSchemaId != excludeId));
        }

        public async Task<ImportSchema?> GetByIdAsync(int id)
        {
            return await _dbContext.ImportSchemas
                .FirstOrDefaultAsync(s => s.ImportSchemaId == id);
        }

        public async Task<ImportSchema?> GetByIdWithVersionsAsync(int id)
        {
            return await _dbContext.ImportSchemas
                .Include(s => s.ImportSchemaVersions)
                .FirstOrDefaultAsync(s => s.ImportSchemaId == id);
        }

        public async Task<ImportSchema?> GetActiveAsync()
        {
            return await _dbContext.ImportSchemas
                .Where(s => s.IsActive == true)
                .Include(s => s.ImportSchemaVersions)
                .FirstOrDefaultAsync();
        }

        // ============ Command Methods ============

        public async Task AddAsync(ImportSchema schema)
        {
            await _dbContext.ImportSchemas.AddAsync(schema);
        }

        public void Update(ImportSchema schema)
        {
            _dbContext.ImportSchemas.Update(schema);
        }

        public void Delete(ImportSchema schema)
        {
            _dbContext.ImportSchemas.Remove(schema);
        }

        public async Task DeactivateAllAsync()
        {
            await _dbContext.ImportSchemas
                .Where(s => s.IsActive == true)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        }
    }
}
