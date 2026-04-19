using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.DTOs.Auth;
using BizFlow.Application.DTOs.Otp;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Login or Register via Google OAuth. Returns tokens + account info.
        /// </summary>
        Task<AuthResponse> GoogleLoginAsync(string idToken, string? deviceInfo);

        /// <summary>
        /// Login using email credential and account password.
        /// </summary>
        Task<AuthResponse> LoginWithEmailAsync(string email, string password, string? deviceInfo);

        /// <summary>
        /// Login using phone credential and account password.
        /// </summary>
        Task<AuthResponse> LoginWithPhoneAsync(string phone, string password, string? deviceInfo);

        /// <summary>
        /// Register using phone + password after Firebase Phone Auth OTP verification.
        /// </summary>
        Task<AuthResponse> RegisterWithPhoneAsync(string phone, string password, string firebaseIdToken, string? fullName, string? deviceInfo);

        /// <summary>
        /// Link phone credential to an existing account after Firebase Phone Auth OTP verification.
        /// </summary>
        Task<List<CredentialInfo>> LinkPhoneAsync(Guid accountId, string phone, string firebaseIdToken, string? password);

        /// <summary>
        /// Set password for an account (Google-only accounts that need a fallback credential).
        /// Also links an email credential using the Google email.
        /// </summary>
        Task SetPasswordAsync(Guid accountId, string password);

        /// <summary>
        /// Change password for an account that already has a password. Revokes all refresh tokens.
        /// When <paramref name="currentPassword"/> is null/empty and <c>MustChangePassword</c> is true, current password is not required (session proves identity).
        /// </summary>
        Task ChangePasswordAsync(Guid accountId, string? currentPassword, string newPassword);

        /// <summary>
        /// Get the signed-in user's profile from storage.
        /// </summary>
        Task<UserProfileDto> GetProfileAsync(Guid profileId);

        /// <summary>
        /// Update the signed-in user's profile (name, tax code).
        /// </summary>
        Task<UserProfileDto> UpdateProfileInfoAsync(Guid profileId, UpdateProfileInfoRequest request);

        /// <summary>
        /// Update the signed-in user's avatar (upload avatar or remove).
        /// </summary>
        Task<UserProfileDto> UpdateAvatarAsync(Guid profileId, UpdateAvatarRequest request);

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
        /// Soft-delete the signed-in account after confirming password (revokes all refresh tokens).
        /// Physical removal and anonymization of audit fields run later via Hangfire using <c>AccountPurge</c> settings.
        /// </summary>
        Task DeleteAccountAsync(Guid accountId, string password);

        /// <summary>
        /// Get linked credentials for an account.
        /// </summary>
        Task<List<CredentialInfo>> GetCredentialsAsync(Guid accountId);

        /// <summary>
        /// Create Firebase custom token for a profile id.
        /// </summary>
        Task<FirebaseCustomTokenResponse> CreateFirebaseCustomTokenAsync(Guid profileId);

        /// <summary>
        /// Verify forgot-password request by either email OTP or Firebase phone token.
        /// </summary>
        Task<VerifyOtpResponse> VerifyOtpForPasswordResetAsync(VerifyOtpRequest request, CancellationToken ct = default);

        /// <summary>
        /// Consume email OTP and return a password-reset access JWT (no refresh token). Forgot-password flow.
        /// </summary>
        Task<VerifyOtpResponse> VerifyEmailOtpForPasswordResetAsync(string email, string otpCode, CancellationToken ct = default);

        /// <summary>
        /// Verify Firebase phone auth token and return a password-reset access JWT (no refresh token). Forgot-password flow.
        /// </summary>
        Task<VerifyOtpResponse> VerifyFirebaseOtpForPasswordResetAsync(string firebaseIdToken, CancellationToken ct = default);

        /// <summary>
        /// Set a new password after forgot-password OTP; revokes all refresh tokens. Caller must use password-reset JWT.
        /// </summary>
        Task ResetPasswordAfterForgotOtpAsync(Guid accountId, string newPassword, Guid passwordResetNonce);

        /// <summary>
        /// Admin-only: create consultant (email + random password, welcome email). Does not return the password.
        /// </summary>
        Task<CreateConsultantResponse> CreateConsultantByAdminAsync(string email, string? fullName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin-only: hard-delete a consultant account (same purge pipeline as user self-delete). Rejects if the target is not a consultant.
        /// </summary>
        Task DeleteConsultantByAdminAsync(Guid accountId);
    }
}
