using System.Security.Claims;

namespace BizFlow.Application.Common.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(Guid accountId, Guid profileId, string roleName);
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
