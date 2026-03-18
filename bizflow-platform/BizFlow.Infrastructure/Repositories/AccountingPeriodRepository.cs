using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories;

public class AccountingPeriodRepository : IAccountingPeriodRepository
{
    private readonly BizFlowDbContext _context;

    public AccountingPeriodRepository(BizFlowDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(int locationId, string periodType, short year, int? quarter)
    {
        return await _context.Set<AccountingPeriod>().AnyAsync(x =>
            x.BusinessLocationId == locationId &&
            x.PeriodType == periodType &&
            x.Year == year &&
            x.Quarter == quarter);
    }

    public async Task<AccountingPeriod?> GetByLocationAndIdAsync(int locationId, long periodId)
    {
        return await _context.Set<AccountingPeriod>()
            .FirstOrDefaultAsync(x => x.BusinessLocationId == locationId && x.PeriodId == periodId);
    }

    public async Task<List<AccountingPeriod>> GetByLocationAsync(int locationId)
    {
        return await _context.Set<AccountingPeriod>()
            .Where(x => x.BusinessLocationId == locationId)
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<AccountingPeriod?> GetPreviousPeriodAsync(int locationId, string periodType, DateOnly currentStartDate)
    {
        return await _context.Set<AccountingPeriod>()
            .Where(x =>
                x.BusinessLocationId == locationId &&
                x.PeriodType == periodType &&
                x.EndDate < currentStartDate)
            .OrderByDescending(x => x.EndDate)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsOverlappingPeriodAsync(int locationId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.Set<AccountingPeriod>().AnyAsync(x =>
            x.BusinessLocationId == locationId &&
            x.StartDate <= endDate &&
            x.EndDate >= startDate);
    }

    public async Task<(decimal NetCash, decimal NetBank)> CalculateNetCashAndBankAsync(int locationId, DateOnly startDate, DateOnly endDate)
    {
        var entries = await _context.Set<GeneralLedgerEntry>()
            .Where(x =>
                x.BusinessLocationId == locationId &&
                x.EntryDate >= startDate &&
                x.EntryDate <= endDate)
            .Select(x => new { x.MoneyChannel, x.DebitAmount, x.CreditAmount })
            .ToListAsync();

        var netCash = entries
            .Where(x => x.MoneyChannel == "cash")
            .Sum(x => x.DebitAmount - x.CreditAmount);

        var netBank = entries
            .Where(x => x.MoneyChannel == "bank")
            .Sum(x => x.DebitAmount - x.CreditAmount);

        return (netCash, netBank);
    }

    public async Task<long> CountActiveBooksAsync(long periodId)
    {
        var hasBookTable = await _context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'AccountingBooks'")
            .FirstAsync();

        if (hasBookTable == 0)
        {
            return 0;
        }

        var count = await _context.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM AccountingBooks WHERE PeriodId = {0} AND Status = 'active'", periodId)
            .FirstAsync();

        return count;
    }

    public async Task AddAsync(AccountingPeriod period)
    {
        await _context.Set<AccountingPeriod>().AddAsync(period);
    }

    public void Update(AccountingPeriod period)
    {
        _context.Set<AccountingPeriod>().Update(period);
    }

    public async Task AddAuditLogAsync(AccountingPeriodAuditLog log)
    {
        await _context.Set<AccountingPeriodAuditLog>().AddAsync(log);
    }

    public async Task<List<AccountingPeriodAuditLog>> GetAuditLogsAsync(long periodId)
    {
        return await _context.Set<AccountingPeriodAuditLog>()
            .Where(x => x.PeriodId == periodId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}