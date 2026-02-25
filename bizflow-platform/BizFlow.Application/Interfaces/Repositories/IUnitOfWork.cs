using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizFlow.Application.Interfaces.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IRoleRepository Roles { get; }
        IBusinessLocationRepository BusinessLocations { get; }
        IHireRepository Hires { get; }
        IProductRepository Products { get; }
        IImportRepository Imports { get; }
        IImportSchemaRepository ImportSchemas { get; }

        //========================================================
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task ExecuteResilientAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
        Task<TResult> ExecuteResilientAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);
    }
}
