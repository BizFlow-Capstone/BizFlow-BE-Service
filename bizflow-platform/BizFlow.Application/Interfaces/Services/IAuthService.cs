using BizFlow.Application.DTOs.Auth;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Login or Register via Google OAuth. Returns tokens + account info.
        /// </summary>
        Task<AuthResponse> GoogleLoginAsync(string idToken, string? deviceInfo);

        /// <summary>
        /// Set password for an account (Google-only accounts that need a fallback credential).
        /// Also links an email credential using the Google email.
        /// </summary>
        Task SetPasswordAsync(Guid accountId, string password);

        /// <summary>
        /// Refresh access token using a valid refresh token.
        /// </summary>
        Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? deviceInfo);

        /// <summary>
        /// Revoke a specific refresh token (logout).
        /// </summary>
        Task RevokeRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Revoke all refresh tokens for an account (logout all devices).
        /// </summary>
        Task RevokeAllRefreshTokensAsync(Guid accountId);

        /// <summary>
        /// Get linked credentials for an account.
        /// </summary>
        Task<List<CredentialInfo>> GetCredentialsAsync(Guid accountId);
    }
}
