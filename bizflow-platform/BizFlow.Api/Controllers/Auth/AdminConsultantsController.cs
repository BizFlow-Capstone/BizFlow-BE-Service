using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers.Auth;

[Route("api/admin/consultants")]
[Authorize]
public class AdminConsultantsController : BaseApiController
{
    private readonly IAuthService _authService;

    public AdminConsultantsController(
        IAuthService authService,
        IMessageService messageService,
        ILogger<AdminConsultantsController> logger)
        : base(messageService, logger)
    {
        _authService = authService;
    }

    /// <summary>Create a consultant account (random password emailed via Resend template <c>consultant-welcome</c>).</summary>
    [HttpPost]
    public async Task<IActionResult> CreateConsultant([FromBody] CreateConsultantRequest request, CancellationToken cancellationToken)
    {
        User.EnsureAdminRole();

        try
        {
            var result = await _authService.CreateConsultantByAdminAsync(request.Email, request.FullName, cancellationToken);
            Logger.LogInformation("Consultant created. AccountId={AccountId}", result.AccountId);
            return Ok(result, MessageKeys.ConsultantCreated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == MessageKeys.EmailAlreadyExists)
        {
            return Conflict(MessageKeys.EmailAlreadyExists);
        }
        catch (InvalidOperationException ex) when (ex.Message == MessageKeys.ConsultantRoleNotFound)
        {
            Logger.LogError("Consultant role missing in database");
            return InternalServerError(ex);
        }
        catch (Exception ex)
        {
            return InternalServerError(ex);
        }
    }
}
