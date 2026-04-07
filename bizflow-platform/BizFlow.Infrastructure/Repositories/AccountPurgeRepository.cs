using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Repositories;

public sealed class AccountPurgeRepository : IAccountPurgeRepository
{
    private readonly BizFlowDbContext _db;
    private readonly ILogger<AccountPurgeRepository> _logger;

    public AccountPurgeRepository(BizFlowDbContext db, ILogger<AccountPurgeRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<List<Guid>> GetPendingPurgeAccountIdsAsync(DateTime eligibilityCutoffUtc, int take, CancellationToken ct = default)
    {
        var batch = Math.Max(1, take);
        return _db.Accounts.AsNoTracking()
            .Where(a => a.DeletedAt != null && a.DeletedAt <= eligibilityCutoffUtc)
            .OrderBy(a => a.DeletedAt)
            .Select(a => a.AccountId)
            .Take(batch)
            .ToListAsync(ct);
    }

    public async Task<(bool Committed, Guid? PurgedProfileId)> TryPurgeOneAccountWithinTransactionAsync(
        Guid accountId,
        DateTime eligibilityCutoffUtc,
        CancellationToken ct = default)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId, ct);
        if (account == null || account.DeletedAt == null)
            return (false, null);

        if (account.DeletedAt > eligibilityCutoffUtc)
            return (false, null);

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.AccountId == accountId, ct);
        if (profile == null)
        {
            await DeleteOtpCodesForAccountAsync(accountId, ct);
            await _db.Accounts.Where(a => a.AccountId == accountId).ExecuteDeleteAsync(ct);
            return (true, null);
        }

        var profileId = profile.ProfileId;

        await AnonymizeProfileReferencesAsync(profileId, ct);

        await _db.Transactions.Where(t => t.ProfileId == profileId).ExecuteDeleteAsync(ct);
        await _db.Subscriptions.Where(s => s.OwnerProfileId == profileId).ExecuteDeleteAsync(ct);

        var ownedLocationIds = await _db.UserLocationAssignments.IgnoreQueryFilters()
            .Where(u => u.UserId == profileId && u.IsOwner)
            .Select(u => u.BusinessLocationId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var locId in ownedLocationIds)
            await DeleteOwnedBusinessLocationAsync(locId, ct);

        await DeleteOtpCodesForAccountAsync(accountId, ct);

        await _db.Accounts.Where(a => a.AccountId == accountId).ExecuteDeleteAsync(ct);

        return (true, profileId);
    }

    private async Task DeleteOtpCodesForAccountAsync(Guid accountId, CancellationToken ct)
    {
        var emails = await _db.Credentials.AsNoTracking()
            .Where(c => c.AccountId == accountId && c.Type == "email")
            .Select(c => c.Identifier)
            .ToListAsync(ct);
        if (emails.Count == 0)
            return;

        await _db.OtpCodes.Where(o => emails.Contains(o.Email)).ExecuteDeleteAsync(ct);
    }

    private async Task AnonymizeProfileReferencesAsync(Guid profileId, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Orders SET CreatedBy = NULL WHERE CreatedBy = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Orders SET UpdatedBy = NULL WHERE UpdatedBy = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Orders SET CompletedBy = NULL WHERE CompletedBy = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Orders SET CancelledBy = NULL WHERE CancelledBy = {profileId}", ct);

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Costs SET CreatedBy = NULL WHERE CreatedBy = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Revenues SET CreatedBy = NULL WHERE CreatedBy = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Debtors SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE DebtorPaymentTransactions SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE TaxPayments SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AccountingBooks SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AccountingExports SET ExportedByUserId = NULL WHERE ExportedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AccountingPeriodAuditLogs SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AccountingPeriods SET FinalizedByUserId = NULL WHERE FinalizedByUserId = {profileId}", ct);

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AccountingBookTaxOverrides SET UpdatedByUserId = NULL WHERE UpdatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE BusinessTypes SET CreatedBy = NULL WHERE CreatedBy = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE BusinessTypes SET ModifiedBy = NULL WHERE ModifiedBy = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE ImportSchemaVersions SET CreatedBy = NULL WHERE CreatedBy = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE NotificationCampaigns SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE SubscriptionAuditLogs SET PerformedBy = NULL WHERE PerformedBy = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE FormulaDefinitions SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE TaxRulesets SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE MappableEntities SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AccountingTemplateVersions SET CreatedByUserId = NULL WHERE CreatedByUserId = {profileId}", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE SystemConfig SET UpdatedBy = NULL WHERE UpdatedBy = {profileId}", ct);
    }

    private async Task DeleteOwnedBusinessLocationAsync(int locationId, CancellationToken ct)
    {
        var bookIds = await _db.AccountingBooks
            .Where(b => b.BusinessLocationId == locationId)
            .Select(b => b.BookId)
            .ToListAsync(ct);

        if (bookIds.Count > 0)
        {
            await _db.FormulaResults.Where(fr => bookIds.Contains(fr.BookId)).ExecuteDeleteAsync(ct);
            await _db.AccountingExports.Where(e => bookIds.Contains(e.BookId)).ExecuteDeleteAsync(ct);
            await _db.AccountingBooks.Where(b => bookIds.Contains(b.BookId)).ExecuteDeleteAsync(ct);
        }

        await _db.TaxPayments.IgnoreQueryFilters()
            .Where(t => t.BusinessLocationId == locationId)
            .ExecuteDeleteAsync(ct);

        var periodIds = await _db.AccountingPeriods
            .Where(p => p.BusinessLocationId == locationId)
            .Select(p => p.PeriodId)
            .ToListAsync(ct);
        if (periodIds.Count > 0)
        {
            await _db.AccountingPeriodAuditLogs.Where(a => periodIds.Contains(a.PeriodId)).ExecuteDeleteAsync(ct);
            await _db.AccountingPeriods.Where(p => periodIds.Contains(p.PeriodId)).ExecuteDeleteAsync(ct);
        }

        var orderIds = await CollectOrderIdsForLocationAsync(locationId, ct);
        await DeleteOrdersInTopologicalOrderAsync(orderIds, ct);

        await _db.Revenues.IgnoreQueryFilters()
            .Where(r => r.BusinessLocationId == locationId)
            .ExecuteDeleteAsync(ct);

        var debtorIds = await _db.Debtors.IgnoreQueryFilters()
            .Where(d => d.BusinessLocationId == locationId)
            .Select(d => d.DebtorId)
            .ToListAsync(ct);
        if (debtorIds.Count > 0)
            await _db.DebtorPaymentTransactions.Where(t => debtorIds.Contains(t.DebtorId)).ExecuteDeleteAsync(ct);

        if (debtorIds.Count > 0)
            await _db.Debtors.IgnoreQueryFilters().Where(d => debtorIds.Contains(d.DebtorId)).ExecuteDeleteAsync(ct);

        while (true)
        {
            var n = await _db.GeneralLedgerEntries
                .Where(g => g.BusinessLocationId == locationId && g.ReversedEntryId != null)
                .ExecuteDeleteAsync(ct);
            if (n == 0)
                break;
        }

        await _db.GeneralLedgerEntries.Where(g => g.BusinessLocationId == locationId).ExecuteDeleteAsync(ct);

        await _db.Costs.IgnoreQueryFilters()
            .Where(c => c.BusinessLocationId == locationId)
            .ExecuteDeleteAsync(ct);

        var importIds = await _db.Imports
            .Where(i => i.BusinessLocationId == locationId)
            .Select(i => i.ImportId)
            .ToListAsync(ct);
        var productIds = await _db.Products.IgnoreQueryFilters()
            .Where(p => p.BusinessLocationId == locationId)
            .Select(p => p.ProductId)
            .ToListAsync(ct);

        if (importIds.Count > 0 || productIds.Count > 0)
        {
            await _db.ProductsImports
                .Where(pi => importIds.Contains(pi.ImportId) || productIds.Contains(pi.ProductId))
                .ExecuteDeleteAsync(ct);
        }

        if (importIds.Count > 0)
            await _db.Imports.Where(i => importIds.Contains(i.ImportId)).ExecuteDeleteAsync(ct);

        if (productIds.Count > 0)
            await _db.Products.IgnoreQueryFilters().Where(p => productIds.Contains(p.ProductId)).ExecuteDeleteAsync(ct);

        await _db.BusinessLocations.IgnoreQueryFilters()
            .Where(l => l.BusinessLocationId == locationId)
            .ExecuteDeleteAsync(ct);
    }

    private async Task<HashSet<long>> CollectOrderIdsForLocationAsync(int locationId, CancellationToken ct)
    {
        var fromDebtors = await _db.Orders
            .Where(o => o.DebtorId != null
                        && _db.Debtors.IgnoreQueryFilters().Any(d =>
                            d.DebtorId == o.DebtorId && d.BusinessLocationId == locationId))
            .Select(o => o.OrderId)
            .ToListAsync(ct);

        var fromProducts = await (
            from o in _db.Orders
            join od in _db.OrderDetails on o.OrderId equals od.OrderId
            join si in _db.SaleItems.IgnoreQueryFilters() on od.SaleItemId equals si.SaleItemId
            join p in _db.Products.IgnoreQueryFilters() on si.ProductId equals p.ProductId
            where p.BusinessLocationId == locationId
            select o.OrderId).Distinct().ToListAsync(ct);

        return fromDebtors.Union(fromProducts).ToHashSet();
    }

    private async Task DeleteOrdersInTopologicalOrderAsync(HashSet<long> orderIds, CancellationToken ct)
    {
        while (orderIds.Count > 0)
        {
            var leafBatch = await _db.Orders
                .Where(o => orderIds.Contains(o.OrderId) && !_db.Orders.Any(x => x.RefOrderId == o.OrderId))
                .Select(o => o.OrderId)
                .Take(200)
                .ToListAsync(ct);

            if (leafBatch.Count == 0)
            {
                _logger.LogWarning(
                    "Order purge: cycle or stuck RefOrder graph; forcing delete of remaining {Count} orders",
                    orderIds.Count);
                await _db.Orders.Where(o => orderIds.Contains(o.OrderId)).ExecuteDeleteAsync(ct);
                break;
            }

            await _db.Orders.Where(o => leafBatch.Contains(o.OrderId)).ExecuteDeleteAsync(ct);
            foreach (var id in leafBatch)
                orderIds.Remove(id);
        }
    }
}
