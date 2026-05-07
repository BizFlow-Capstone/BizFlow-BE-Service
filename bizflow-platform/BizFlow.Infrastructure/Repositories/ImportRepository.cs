using BizFlow.Application.DTOs.Import;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using BizFlow.Application.Specifications.Imports;
using BizFlow.Infrastructure.Specifications;
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
            // 1. Get total count
            var countSpec = new ImportSearchSpec(query, isCount: true);
            var countQuery = SpecificationEvaluator<Import>.GetQuery(_dbContext.Imports.AsQueryable(), countSpec);
            var totalCount = await countQuery.CountAsync();

            if (totalCount == 0)
                return (Array.Empty<Import>(), 0);

            // 2. Deferred Join Strategy (Get IDs first)
            var filterSpec = new ImportSearchSpec(query, isCount: false, filterOnly: true);
            var filterQuery = SpecificationEvaluator<Import>.GetQuery(_dbContext.Imports.AsQueryable(), filterSpec);

            var pageNumber = query.PageNumber ?? 1;
            var pageSize = query.PageSize ?? 10;

            var ids = await filterQuery
                .Select(i => i.ImportId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!ids.Any())
                return (Array.Empty<Import>(), totalCount);

            // 3. Fetch full entities
            var items = await _dbContext.Imports
                .Where(i => ids.Contains(i.ImportId))
                .OrderByDescending(i => i.CreatedAt)
                .Include(i => i.BusinessLocation)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Import?> GetByIdAsync(long importId)
        {
            return await _dbContext.Imports
                .FirstOrDefaultAsync(i => i.ImportId == importId);
        }

        public async Task<List<Import>> GetByIdsAsync(IEnumerable<long> importIds)
        {
            var ids = importIds.Distinct().ToList();
            if (ids.Count == 0)
                return new List<Import>();

            return await _dbContext.Imports
                .Where(i => ids.Contains(i.ImportId))
                .ToListAsync();
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

        public async Task<HashSet<string>> GetExistingPublicIdsAsync(IEnumerable<string> publicIds)
        {
            var ids = publicIds.ToList();
            if (ids.Count == 0) return new HashSet<string>();

            var existing = await _dbContext.Imports
                .Where(i => i.ImagePublicId != null && ids.Contains(i.ImagePublicId))
                .Select(i => i.ImagePublicId!)
                .ToListAsync();

            return new HashSet<string>(existing);
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

        public Task LockImportRowForUpdateAsync(long importId, CancellationToken cancellationToken = default)
            => _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT ImportId FROM `Imports` WHERE ImportId = {0} LIMIT 1 FOR UPDATE",
                new object[] { importId },
                cancellationToken);

        public Task<Import?> GetLatestReplacementByRefImportIdAsync(long refImportId, CancellationToken cancellationToken = default)
            => _dbContext.Imports
                .Where(i => i.RefImportId == refImportId)
                .OrderByDescending(i => i.ImportId)
                .FirstOrDefaultAsync(cancellationToken);

        public Task<Import?> GetLatestReplacementByRefImportIdAsync(
            long refImportId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => _dbContext.Imports
                .Where(i => i.RefImportId == refImportId && i.IdempotencyKey == idempotencyKey)
                .OrderByDescending(i => i.ImportId)
                .FirstOrDefaultAsync(cancellationToken);

        public void Delete(Import import)
        {
            _dbContext.Imports.Remove(import);
        }

        public void DeleteItem(ProductImport item)
        {
            _dbContext.ProductsImports.Remove(item);
        }

        public async Task<Dictionary<(long ImportId, long ProductId), decimal>> GetImportCostLookupByLocationAsync(int locationId)
        {
            var items = await _dbContext.ProductsImports
                .Where(pi => pi.Import.BusinessLocationId == locationId)
                .Select(pi => new { pi.ImportId, pi.ProductId, pi.CostPrice })
                .ToListAsync();

            return items.ToDictionary(
                x => (x.ImportId, x.ProductId),
                x => x.CostPrice);
        }
    }
}
