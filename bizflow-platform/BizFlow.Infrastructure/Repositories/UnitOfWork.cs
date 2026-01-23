using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizFlow.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BizFlowDbContext _dbContext;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(BizFlowDbContext dbContext,
            IRoleRepository roleRepository)
        {
            _dbContext = dbContext;
            Roles = roleRepository;
        }

        public IRoleRepository Roles { get; set; }

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
