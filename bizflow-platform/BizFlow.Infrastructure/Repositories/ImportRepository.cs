using BizFlow.Application.DTOs.Import;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories
{
    public class ImportRepository : IImportRepository
    {
        private readonly BizFlowDbContext _dbContext;

        public ImportRepository(BizFlowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ============ Query Methods ============

        public async Task<(IEnumerable<Import> Items, int TotalCount)> SearchAsync(ImportQueryParams query)
        {
            var baseQuery = _dbContext.Imports
                .Include(i => i.BusinessLocation)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(query.Status))
                baseQuery = baseQuery.Where(i => i.Status == query.Status);

            if (!string.IsNullOrWhiteSpace(query.ImportType))
                baseQuery = baseQuery.Where(i => i.ImportType == query.ImportType);

            if (query.BusinessLocationId.HasValue)
                baseQuery = baseQuery.Where(i => i.BusinessLocationId == query.BusinessLocationId.Value);

            if (query.FromDate.HasValue)
                baseQuery = baseQuery.Where(i => i.CreatedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                baseQuery = baseQuery.Where(i => i.CreatedAt <= query.ToDate.Value);

            var totalCount = await baseQuery.CountAsync();

            if (totalCount == 0)
                return (new List<Import>(), 0);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;

            var items = await baseQuery
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Import?> GetByIdAsync(long importId)
        {
            return await _dbContext.Imports
                .FirstOrDefaultAsync(i => i.ImportId == importId);
        }

        public async Task<Import?> GetByIdWithItemsAsync(long importId)
        {
            return await _dbContext.Imports
                .Where(i => i.ImportId == importId)
                .Include(i => i.BusinessLocation)
                .Include(i => i.ProductsImports)
                    .ThenInclude(pi => pi.Product)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _dbContext.Imports.CountAsync();
        }

        // ============ Command Methods ============

        public async Task<Import> AddAsync(Import import)
        {
            var entry = await _dbContext.Imports.AddAsync(import);
            return entry.Entity;
        }

        public void Update(Import import)
        {
            _dbContext.Imports.Update(import);
        }

        public void Delete(Import import)
        {
            _dbContext.Imports.Remove(import);
        }

        public void DeleteItem(ProductImport item)
        {
            _dbContext.ProductsImports.Remove(item);
        }
    }
}
