using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using System.Text.Json;

namespace BizFlow.Api.Common.Middleware;

/// <summary>
/// Password-reset JWTs (purpose claim) may only call the forgot-password reset endpoint.
/// </summary>
public class PasswordResetTokenRestrictionMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly PathString ForgotPasswordResetPath = new("/api/auth/forgot-password/reset");

    public PasswordResetTokenRestrictionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMessageService messageService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var purpose = context.User.FindFirst(AuthJwtConstants.PurposeClaimType)?.Value;
            if (string.Equals(purpose, AuthJwtConstants.PasswordResetPurpose, StringComparison.Ordinal))
            {
                if (!context.Request.Path.StartsWithSegments(ForgotPasswordResetPath))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var messageKey = MessageKeys.Forbidden;
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

public static class PasswordResetTokenRestrictionMiddlewareExtensions
{
    public static IApplicationBuilder UsePasswordResetTokenRestriction(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PasswordResetTokenRestrictionMiddleware>();
    }
}
