using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using System.Text.Json;

namespace BizFlow.Api.Common.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public JwtAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMessageService messageService)
    {
        await _next(context);

        // Only handle 401 responses that have not started writing a body
        if (context.Response.StatusCode == 401 && !context.Response.HasStarted)
        {
            // Check IS-TOKEN-EXPIRED header
            var isTokenExpired = context.Response.Headers.ContainsKey("IS-TOKEN-EXPIRED");

            context.Response.ContentType = "application/json";

            var messageKey = isTokenExpired ? MessageKeys.TokenExpired : MessageKeys.Unauthorized;

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
        }
    }
}

public static class JwtAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuthenticationMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}