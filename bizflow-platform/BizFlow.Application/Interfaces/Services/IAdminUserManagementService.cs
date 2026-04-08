using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Admin;

namespace BizFlow.Application.Interfaces.Services;

public interface IAdminUserManagementService
{
    Task<PaginatedResponse<AdminManagedUserDto>> GetUsersAsync(AdminUserQueryParams query);
    Task RevokeAllRefreshTokensAsync(Guid accountId);
}
