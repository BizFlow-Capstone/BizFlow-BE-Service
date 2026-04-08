using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BizFlow.Api.Controllers.Auth;

[Route("api/admin/users")]
[Authorize]
public class AdminUsersController : PaginatedApiController
{
    private readonly IAdminUserManagementService _adminUserManagementService;

    public AdminUsersController(
        IAdminUserManagementService adminUserManagementService,
        IMessageService messageService,
        IOptions<PaginationSettings> paginationSettings,
        ILogger<AdminUsersController> logger)
        : base(messageService, logger, paginationSettings)
    {
        _adminUserManagementService = adminUserManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserQueryParams query)
    {
        User.EnsureAdminRole();
        ApplyPaginationDefaults(query);
        var result = await _adminUserManagementService.GetUsersAsync(query);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpDelete("{accountId:guid}/refresh-tokens")]
    public async Task<IActionResult> RevokeAllRefreshTokens(Guid accountId)
    {
        User.EnsureAdminRole();
        await _adminUserManagementService.RevokeAllRefreshTokensAsync(accountId);
        return Ok(MessageKeys.AdminRevokeAllRefreshTokensSuccess);
    }
}
