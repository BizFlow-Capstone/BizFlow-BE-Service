using Hangfire.Dashboard;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace BizFlow.Api.Common.Filters;

/// <summary>
/// Allows Hangfire Dashboard access only to requests carrying a valid JWT
/// that has the "Admin" role claim. Use in production instead of
/// LocalRequestsOnlyAuthorizationFilter.
///
/// How to access in browser:
///   GET https://api.bizflow.asia/hangfire
///   Add cookie: HangfireToken=<your_admin_jwt>
/// </summary>
public sealed class AdminJwtHangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _jwtSecret;
    private readonly string _issuer;
    private readonly string _audience;

    public AdminJwtHangfireDashboardAuthorizationFilter(string jwtSecret, string issuer, string audience)
    {
        _jwtSecret = jwtSecret;
        _issuer = issuer;
        _audience = audience;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Read token from cookie (set manually in browser)
        var token = httpContext.Request.Cookies["HangfireToken"]
                 ?? httpContext.Request.Headers["Authorization"].ToString()
                               .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                               .Trim();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            }, out var validatedToken);

            var jwt = (JwtSecurityToken)validatedToken;
            return jwt.Claims.Any(c =>
                (string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(c.Type, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.OrdinalIgnoreCase))
                && string.Equals(c.Value, "admin", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
