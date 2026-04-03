using BizFlow.Application.Common.Models;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;

namespace BizFlow.Infrastructure.Services;

public class AdminUserManagementService : IAdminUserManagementService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminUserManagementService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginatedResponse<AdminManagedUserDto>> GetUsersAsync(AdminUserQueryParams query)
    {
        var pageNumber = query.PageNumber ?? 1;
        var pageSize = query.PageSize ?? 10;

        var (accounts, totalCount) = await _unitOfWork.Accounts.SearchAdminManagedUsersAsync(query);

        var items = accounts.Select(a => new AdminManagedUserDto
        {
            AccountId = a.AccountId,
            ProfileId = a.Profile != null ? a.Profile.ProfileId : Guid.Empty,
            FullName = a.Profile != null ? a.Profile.FullName : string.Empty,
            Role = a.Role.Name,
            IsActive = a.IsActive ?? false,
            Email = a.Credentials
                .Where(c => c.Type == "email")
                .Select(c => c.Identifier)
                .FirstOrDefault(),
            Phone = a.Credentials
                .Where(c => c.Type == "phone")
                .Select(c => c.Identifier)
                .FirstOrDefault(),
            LastLoginAt = a.LastLoginAt,
            CreatedAt = a.CreatedAt
        }).ToList();

        return new PaginatedResponse<AdminManagedUserDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task RevokeAllRefreshTokensAsync(Guid accountId)
    {
        var account = await _unitOfWork.Accounts.GetNonAdminAccountByIdAsync(accountId)
            ?? throw new NotFoundException(MessageKeys.AccountNotFound);

        var now = DateTime.UtcNow;
        var tokens = await _unitOfWork.Accounts.GetUnexpiredRefreshTokensAsync(accountId, now);

        foreach (var token in tokens)
        {
            token.ExpiresAt = now;
            token.RevokedAt = now;
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
