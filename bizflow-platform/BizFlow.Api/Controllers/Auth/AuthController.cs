using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Auth;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BizFlow.Api.Controllers.Auth
{
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(
            IAuthService authService,
            IMessageService messageService,
            ILogger<AuthController> logger)
            : base(messageService, logger)
        {
            _authService = authService;
        }

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var result = await _authService.GoogleLoginAsync(request.IdToken, request.DeviceInfo);
                var messageKey = result.IsNewAccount ? MessageKeys.AccountCreated : MessageKeys.LoginSuccess;
                return Ok(result, messageKey);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(MessageKeys.InvalidGoogleToken);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost("set-password")]
        [Authorize]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                await _authService.SetPasswordAsync(accountId, request.Password);
                return Ok(MessageKeys.PasswordSet);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(MessageKeys.AccountNotFound);
            }
            catch (InvalidOperationException)
            {
                return BadRequest(MessageKeys.PasswordAlreadySet);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken, request.DeviceInfo);
                return Ok(result, MessageKeys.TokenRefreshed);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(MessageKeys.InvalidToken);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
                return Ok(MessageKeys.LogoutSuccess);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet("credentials")]
        [Authorize]
        public async Task<IActionResult> GetCredentials()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var result = await _authService.GetCredentialsAsync(accountId);
                return Ok(result, MessageKeys.DataRetrievedSuccessfully);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private Guid GetCurrentAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Account ID not found in token");
            return Guid.Parse(claim.Value);
        }
    }
}
