using System.Security.Claims;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;

namespace BizFlow.Api.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool IsAuthenticated(this ClaimsPrincipal? principal)
    {
        return principal?.Identity?.IsAuthenticated == true;
    }

    public static string? GetClaimValue(this ClaimsPrincipal? principal, params string[] claimTypes)
    {
        if (principal == null || claimTypes.Length == 0)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public static Guid GetRequiredAccountId(this ClaimsPrincipal? principal)
    {
        EnsureAuthenticated(principal);

        var value = principal.GetClaimValue(ClaimTypes.NameIdentifier, ClaimTypes.Sid, ClaimTypes.Name, "sub")
            ?? throw new UnauthorizedException(MessageKeys.Unauthorized);

        if (!Guid.TryParse(value, out var accountId))
        {
            throw new UnauthorizedException(MessageKeys.Unauthorized);
        }

        return accountId;
    }

    public static Guid GetRequiredUserId(this ClaimsPrincipal? principal)
    {
        EnsureAuthenticated(principal);

        // In this system, user id is profile id. Do not fall back to sub/nameidentifier
        // because those represent account id and can cause authorization mismatches.
        var value = principal.GetClaimValue("profileId", "userId")
            ?? throw new UnauthorizedException(MessageKeys.Unauthorized);

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedException(MessageKeys.Unauthorized);
        }

        return userId;
    }

    public static string? GetRole(this ClaimsPrincipal? principal)
    {
        return principal.GetClaimValue(ClaimTypes.Role, "role");
    }

    public static bool HasRole(this ClaimsPrincipal? principal, string role)
    {
        if (principal == null || string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return principal.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") &&
            string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureAuthenticated(ClaimsPrincipal? principal)
    {
        if (!principal.IsAuthenticated())
        {
            throw new UnauthorizedException(MessageKeys.Unauthorized);
        }
    }
}