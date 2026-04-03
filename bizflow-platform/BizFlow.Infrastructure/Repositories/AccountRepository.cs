using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Specifications.AdminUsers;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using BizFlow.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly BizFlowDbContext _dbContext;

    public AccountRepository(BizFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IEnumerable<Account> Items, int TotalCount)> SearchAdminManagedUsersAsync(AdminUserQueryParams query)
    {
        var countSpec = new AdminUserSearchSpec(query, isCount: true);
        var countQuery = SpecificationEvaluator<Account>.GetQuery(_dbContext.Accounts.AsQueryable(), countSpec);
        var totalCount = await countQuery.CountAsync();

        if (totalCount == 0)
        {
            return (new List<Account>(), 0);
        }

        var searchSpec = new AdminUserSearchSpec(query);
        var searchQuery = SpecificationEvaluator<Account>.GetQuery(_dbContext.Accounts.AsQueryable(), searchSpec);
        var items = await searchQuery
            .Include(a => a.Profile)
            .Include(a => a.Credentials)
            .Include(a => a.Role)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Account?> GetNonAdminAccountByIdAsync(Guid accountId)
    {
        return _dbContext.Accounts
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a =>
                a.AccountId == accountId &&
                a.DeletedAt == null &&
                a.Role.Name.ToLower() != "admin");
    }

    public Task<List<RefreshToken>> GetUnexpiredRefreshTokensAsync(Guid accountId, DateTime nowUtc)
    {
        return _dbContext.RefreshTokens
            .Where(rt => rt.AccountId == accountId && rt.ExpiresAt > nowUtc)
            .ToListAsync();
    }
}
