using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace BizFlow.Api.Common.Middleware;

/// <summary>
/// After JWT is authenticated, ensure the account still exists and is not deleted / disabled.
/// </summary>
public class ActiveAccountMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveAccountMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, BizFlowDbContext db, IMessageService messageService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var accountIdClaim =
                context.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub);

            if (accountIdClaim != null && Guid.TryParse(accountIdClaim.Value, out var accountId))
            {
                var account = await db.Accounts
                    .AsNoTracking()
                    .Select(a => new { a.AccountId, a.IsActive, a.DeletedAt })
                    .FirstOrDefaultAsync(a => a.AccountId == accountId);

                if (account == null || account.DeletedAt != null || account.IsActive == false)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var messageKey = MessageKeys.AccountInactiveOrDeleted;
                    var response = new ApiResponse
                    {
                        Success = false,
                        MessageCode = messageKey,
                        Message = messageService.GetMessage(messageKey),
                        Timestamp = DateTime.UtcNow
                    };

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
                    return;
                }
            }
        }

        await _next(context);
    }
}

public static class ActiveAccountMiddlewareExtensions
{
    public static IApplicationBuilder UseActiveAccountMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ActiveAccountMiddleware>();
    }
}
