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

        // ── Accounting Book Module ──
        IAccountingBookRepository AccountingBooks { get; }
        IAccountingTemplateRepository AccountingTemplates { get; }
        ITaxRulesetRepository TaxRulesets { get; }
        IFormulaDefinitionRepository FormulaDefinitions { get; }
        IFormulaResultRepository FormulaResults { get; }
        IStockMovementRepository StockMovements { get; }
        IAccountRepository Accounts { get; }
        IOtpCodeRepository OtpCodes { get; }

        //========================================================
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task ExecuteResilientAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
        Task<TResult> ExecuteResilientAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);

        /// <summary>
        /// Resilient transaction for bulk SQL (ExecuteDelete / raw UPDATE) without an implicit SaveChanges.
        /// Commits when <paramref name="action"/> returns <c>true</c>, rolls back when <c>false</c>.
        /// </summary>
        Task ExecuteResilientPurgeAsync(Func<CancellationToken, Task<bool>> action, CancellationToken ct = default);
    }
}
