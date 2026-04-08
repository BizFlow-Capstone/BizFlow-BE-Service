using System.Security.Claims;

namespace BizFlow.Application.Common.Interfaces
{
    public interface IJwtService
    {
        /// <param name="forPasswordReset">When true, adds <c>purpose=password_reset</c> and nonce claim for forgot-password flow.</param>
        /// <param name="passwordResetNonce">Required when <paramref name="forPasswordReset"/> is true.</param>
        string GenerateAccessToken(Guid accountId, Guid profileId, string roleName, bool forPasswordReset = false, Guid? passwordResetNonce = null);
        string GenerateRefreshToken();
        string HashToken(string token, string salt);
        string GenerateSalt();
        DateTime GetAccessTokenExpiry();
        DateTime GetRefreshTokenExpiry();
        ClaimsPrincipal? ValidateToken(string token);
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}
