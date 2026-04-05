using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Auth;
using BizFlow.Application.DTOs.Otp;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace BizFlow.Api.Controllers.Auth
{
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly IOtpService _otpService;

        public AuthController(
            IAuthService authService,
            IOtpService otpService,
            IMessageService messageService,
            ILogger<AuthController> logger)
            : base(messageService, logger)
        {
            _authService = authService;
            _otpService = otpService;
        }

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.IdToken))
                {
                    return BadRequest(MessageKeys.ValidationError, new { field = "idToken", message = "Id token is required" });
                }

                var result = await _authService.GoogleLoginAsync(request.IdToken, request.DeviceInfo);
                var messageKey = result.IsNewAccount ? MessageKeys.AccountCreated : MessageKeys.LoginSuccess;
                Logger.LogInformation("Google login success. AccountId={AccountId}, IsNewAccount={IsNewAccount}", result.Account.AccountId, result.IsNewAccount);
                return Ok(result, messageKey);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogWarning("Google login failed due to invalid Google token");
                return Unauthorized(MessageKeys.InvalidGoogleToken);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost("register/phone")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterWithPhone([FromBody] RegisterWithPhoneRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FirebaseIdToken))
                {
                    return BadRequest(MessageKeys.ValidationError, new { message = "Phone, password and firebaseIdToken are required" });
                }

                var result = await _authService.RegisterWithPhoneAsync(request.Phone, request.Password, request.FirebaseIdToken, request.FullName, request.DeviceInfo);
                Logger.LogInformation("Phone register success. AccountId={AccountId}", result.Account.AccountId);
                return Ok(result, MessageKeys.PhoneRegisterSuccess);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(MessageKeys.InvalidFirebaseToken);
            }
            catch (InvalidOperationException ex) when (ex.Message == MessageKeys.PhoneAlreadyExists)
            {
                return Conflict(MessageKeys.PhoneAlreadyExists);
            }
            catch (InvalidOperationException ex) when (ex.Message == MessageKeys.PhoneVerificationMismatch)
            {
                return BadRequest(MessageKeys.PhoneVerificationMismatch);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost("link/phone")]
        [Authorize]
        public async Task<IActionResult> LinkPhone([FromBody] LinkPhoneRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.FirebaseIdToken))
                {
                    return BadRequest(MessageKeys.ValidationError, new { message = "Phone and firebaseIdToken are required" });
                }

                var accountId = GetCurrentAccountId();
                var result = await _authService.LinkPhoneAsync(accountId, request.Phone, request.FirebaseIdToken, request.Password);
                return Ok(result, MessageKeys.PhoneLinkSuccess);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(MessageKeys.InvalidFirebaseToken);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(MessageKeys.AccountNotFound);
            }
            catch (InvalidOperationException ex) when (ex.Message == MessageKeys.PhoneAlreadyLinked)
            {
                return Conflict(MessageKeys.PhoneAlreadyLinked);
            }
            catch (InvalidOperationException ex) when (ex.Message == MessageKeys.PhoneAlreadyExists)
            {
                return Conflict(MessageKeys.PhoneAlreadyExists);
            }
            catch (InvalidOperationException ex) when (ex.Message == MessageKeys.PhoneVerificationMismatch)
            {
                return BadRequest(MessageKeys.PhoneVerificationMismatch);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>Step 1: send OTP to email for forgot-password flow.</summary>
        [HttpPost("forgot-password/send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendForgotPasswordOtp([FromBody] SendOtpRequest request, CancellationToken ct)
        {
            var response = await _otpService.SendOtpAsync(request, ct);
            return Ok(response, MessageKeys.OtpSent);
        }

        /// <summary>Step 2: verify email OTP; returns password-reset access token (no refresh).</summary>
        [HttpPost("forgot-password/verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] VerifyOtpRequest request, CancellationToken ct)
        {
            var response = await _authService.VerifyEmailOtpForPasswordResetAsync(request.Email, request.OtpCode, ct);
            return Ok(response, MessageKeys.OtpVerified);
        }

        /// <summary>Step 3: set new password; requires password-reset JWT from <c>forgot-password/verify-otp</c>.</summary>
        [HttpPost("forgot-password/reset")]
        [Authorize(Policy = AuthJwtConstants.Policies.PasswordReset)]
        public async Task<IActionResult> ResetPasswordForgotFlow([FromBody] SetPasswordRequest request)
        {
            var accountId = GetCurrentAccountId();
            var nonceClaim = User.FindFirst(AuthJwtConstants.PasswordResetNonceClaimType)?.Value;
            if (string.IsNullOrEmpty(nonceClaim) || !Guid.TryParse(nonceClaim, out var passwordResetNonce))
                throw new BadRequestException(MessageKeys.PasswordResetTokenInvalidOrUsed);

            await _authService.ResetPasswordAfterForgotOtpAsync(accountId, request.Password, passwordResetNonce);
            Logger.LogInformation("Forgot password completed. AccountId={AccountId}", accountId);
            return Ok(MessageKeys.PasswordChanged);
        }

        [HttpPost("login/email")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithEmail([FromBody] LoginWithEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(MessageKeys.ValidationError, new { message = "Email and password are required" });
                }

                var result = await _authService.LoginWithEmailAsync(request.Email, request.Password, request.DeviceInfo);
                Logger.LogInformation("Email login success. AccountId={AccountId}", result.Account.AccountId);
                return Ok(result, MessageKeys.LoginSuccess);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogWarning("Email login failed due to invalid credentials");
                return Unauthorized(MessageKeys.InvalidCredentials);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost("login/phone")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithPhone([FromBody] LoginWithPhoneRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(MessageKeys.ValidationError, new { message = "Phone and password are required" });
                }

                var result = await _authService.LoginWithPhoneAsync(request.Phone, request.Password, request.DeviceInfo);
                Logger.LogInformation("Phone login success. AccountId={AccountId}", result.Account.AccountId);
                return Ok(result, MessageKeys.LoginSuccess);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogWarning("Phone login failed due to invalid credentials");
                return Unauthorized(MessageKeys.InvalidCredentials);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
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
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(MessageKeys.ValidationError, new { field = "password", message = MessageKeys.PasswordRequired });
                }

                var accountId = GetCurrentAccountId();
                await _authService.SetPasswordAsync(accountId, request.Password);
                Logger.LogInformation("Set password success. AccountId={AccountId}", accountId);
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
            catch (ArgumentException)
            {
                return BadRequest(MessageKeys.PasswordInvalidFormat);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(MessageKeys.ValidationError, new { message = "Current password and new password are required" });
                }

                var accountId = GetCurrentAccountId();
                await _authService.ChangePasswordAsync(accountId, request.CurrentPassword, request.NewPassword);
                Logger.LogInformation("Change password success. AccountId={AccountId}", accountId);
                return Ok(MessageKeys.PasswordChanged);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(MessageKeys.AccountNotFound);
            }
            catch (InvalidOperationException ex) when (ex.Message == MessageKeys.NoPasswordToChange)
            {
                return BadRequest(MessageKeys.NoPasswordToChange);
            }
            catch (InvalidOperationException ex) when (ex.Message == MessageKeys.NewPasswordSameAsCurrent)
            {
                return BadRequest(MessageKeys.NewPasswordSameAsCurrent);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message == MessageKeys.CurrentPasswordIncorrect)
            {
                Logger.LogWarning("Change password failed due to incorrect current password");
                return Unauthorized(MessageKeys.CurrentPasswordIncorrect);
            }
            catch (ArgumentException ex) when (ex.Message == MessageKeys.PasswordInvalidFormat)
            {
                return BadRequest(MessageKeys.PasswordInvalidFormat);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var profileId = User.GetRequiredUserId();
                var result = await _authService.GetProfileAsync(profileId);
                Logger.LogInformation("Get profile success. ProfileId={ProfileId}", profileId);
                return Ok(result, MessageKeys.DataRetrievedSuccessfully);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(MessageKeys.NotFound);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Update profile info using JSON body only (fullName, taxCode).
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateProfileInfoJson([FromBody] UpdateProfileInfoRequest request)
        {
            try
            {
                var profileId = User.GetRequiredUserId();
                var result = await _authService.UpdateProfileInfoAsync(profileId, request);
                Logger.LogInformation("Update profile info success. ProfileId={ProfileId}", profileId);
                return Ok(result, MessageKeys.DataUpdatedSuccessfully);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(MessageKeys.NotFound);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
            }
            catch (BadRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Update avatar using JSON body only (removeAvatar).
        /// </summary>
        [HttpPut("profile/avatar")]
        [Authorize]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateAvatarJson([FromBody] UpdateAvatarRequest request)
        {
            try
            {
                var profileId = User.GetRequiredUserId();
                var result = await _authService.UpdateAvatarAsync(profileId, request);
                Logger.LogInformation("Update avatar success. ProfileId={ProfileId}", profileId);
                return Ok(result, MessageKeys.DataUpdatedSuccessfully);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(MessageKeys.NotFound);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
            }
            catch (BadRequestException)
            {
                throw;
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
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return BadRequest(MessageKeys.ValidationError, new { field = "refreshToken", message = "Refresh token is required" });
                }

                var result = await _authService.RefreshTokenAsync(request.RefreshToken, request.DeviceInfo);
                Logger.LogInformation("Refresh token success. AccountId={AccountId}", result.Account.AccountId);
                return Ok(result, MessageKeys.TokenRefreshed);
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogWarning("Refresh token failed due to invalid token");
                return Unauthorized(MessageKeys.InvalidToken);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
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
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return BadRequest(MessageKeys.ValidationError, new { field = "refreshToken", message = "Refresh token is required" });
                }

                await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
                Logger.LogInformation("Logout success for one session");
                return Ok(MessageKeys.LogoutSuccess);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(MessageKeys.InvalidToken);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(MessageKeys.ValidationError, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                await _authService.RevokeAllRefreshTokensAsync(accountId);
                Logger.LogInformation("Logout all success. AccountId={AccountId}", accountId);
                return Ok(MessageKeys.LogoutAllSuccess);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Soft-delete immediately; Hangfire purges the row and owned locations after <c>AccountPurge:RetentionDays</c>,
        /// and nulls audit user ids on shared business rows (orders, costs, etc.).
        /// </summary>
        [HttpPost("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(MessageKeys.ValidationError, new { field = "password", message = MessageKeys.PasswordRequired });
                }

                var accountId = GetCurrentAccountId();
                await _authService.DeleteAccountAsync(accountId, request.Password);
                Logger.LogInformation("Delete account success. AccountId={AccountId}", accountId);
                return Ok(MessageKeys.DataDeletedSuccessfully);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(MessageKeys.AccountNotFound);
            }
            catch (InvalidOperationException ex) when (ex.Message == MessageKeys.NoPasswordToDelete)
            {
                return BadRequest(MessageKeys.NoPasswordToDelete);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message == MessageKeys.CurrentPasswordIncorrect)
            {
                Logger.LogWarning("Delete account failed due to incorrect password");
                return Unauthorized(MessageKeys.CurrentPasswordIncorrect);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message == MessageKeys.AccountInactiveOrDeleted)
            {
                return Unauthorized(MessageKeys.AccountInactiveOrDeleted);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(MessageKeys.Unauthorized);
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
                Logger.LogInformation("Get credentials success. AccountId={AccountId}, Count={Count}", accountId, result.Count);
                return Ok(result, MessageKeys.DataRetrievedSuccessfully);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost("firebase/custom-token")]
        [Authorize]
        public async Task<IActionResult> CreateFirebaseCustomToken()
        {
            try
            {
                var profileId = User.GetRequiredUserId();
                var result = await _authService.CreateFirebaseCustomTokenAsync(profileId);
                return Ok(result, MessageKeys.DataRetrievedSuccessfully);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(MessageKeys.NotFound);
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
