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
        IBusinessTypeRepository BusinessTypes { get; }
        IBusinessLocationRepository BusinessLocations { get; }
        IAccountingPeriodRepository AccountingPeriods { get; }
        IHireRepository Hires { get; }
        IProductRepository Products { get; }
        IImportRepository Imports { get; }
        IImportSchemaRepository ImportSchemas { get; }
        IDebtorRepository Debtors { get; }
        ICostRepository Costs { get; }
        IGeneralLedgerRepository GeneralLedgerEntries { get; }
        IRevenueRepository Revenues { get; }
        IOrderRepository Orders { get; }
        IOrderDetailRepository OrderDetails { get; }
        IProfileRepository Profiles { get; }
        ISubscriptionRepository Subscriptions { get; }
        ISubscriptionPlanRepository SubscriptionPlans { get; }
        ITransactionRepository Transactions { get; }
        IFeatureUsageRepository FeatureUsages { get; }
        ISubscriptionPlanPriceRepository PlanPrices { get; }
        IFeatureRepository Features { get; }
        ISubscriptionAuditLogRepository SubscriptionAuditLogs { get; }

        //========================================================
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task ExecuteResilientAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
        Task<TResult> ExecuteResilientAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);
    }
}
