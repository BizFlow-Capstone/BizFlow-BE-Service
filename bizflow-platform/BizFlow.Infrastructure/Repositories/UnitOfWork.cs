using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BizFlow.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BizFlowDbContext _dbContext;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(BizFlowDbContext dbContext,
            IRoleRepository roleRepository,
            IBusinessTypeRepository businessTypeRepository,
            IBusinessLocationRepository businessLocationRepository,
            IHireRepository hireRepository,
            IProductRepository productRepository,
            IImportRepository importRepository,
            IImportSchemaRepository importSchemaRepository,
            IDebtorRepository debtorRepository,
            ICostRepository costRepository)
        {
            _dbContext = dbContext;
            Roles = roleRepository;
            BusinessTypes = businessTypeRepository;
            BusinessLocations = businessLocationRepository;
            Hires = hireRepository;
            Products = productRepository;
            Imports = importRepository;
            ImportSchemas = importSchemaRepository;
            Debtors = debtorRepository;
            Costs = costRepository;
        }

        public IRoleRepository Roles { get; set; }
        public IBusinessTypeRepository BusinessTypes { get; set; }
        public IBusinessLocationRepository BusinessLocations { get; set; }
        public IHireRepository Hires { get; set; }
        public IProductRepository Products { get; set; }
        public IImportRepository Imports { get; set; }
        public IImportSchemaRepository ImportSchemas { get; set; }
        public IDebtorRepository Debtors { get; set; }
        public ICostRepository Costs { get; set; }

        //============================================
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _dbContext.SaveChangesAsync();
                if (_transaction != null)
                    await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_transaction != null)
                    await _transaction.RollbackAsync();
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            await _dbContext.DisposeAsync();
        }

        public async Task ExecuteResilientAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
                await action(ct);
                await SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            });
        }

        public async Task<TResult> ExecuteResilientAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
                var result = await action(ct);
                await SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return result;
            });
        }
    }
}
