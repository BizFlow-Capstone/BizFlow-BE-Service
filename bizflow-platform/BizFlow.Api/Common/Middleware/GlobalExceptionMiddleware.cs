using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using System.Net;
using System.Text.Json;

namespace BizFlow.Api.Common.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, IMessageService messageService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, messageService);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IMessageService messageService)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";

        var response = new ApiResponse();

        switch (exception)
        {
            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Success = false;
                response.MessageCode = notFoundEx.MessageKey;
                response.Message = messageService.GetMessage(notFoundEx.MessageKey, notFoundEx.Args);
                break;

            case BadRequestException badRequestEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Success = false;
                response.MessageCode = badRequestEx.MessageKey;
                response.Message = messageService.GetMessage(badRequestEx.MessageKey, badRequestEx.Args);
                response.Errors = badRequestEx.Errors;
                break;

            case ConflictException conflictEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Success = false;
                response.MessageCode = conflictEx.MessageKey;
                response.Message = messageService.GetMessage(conflictEx.MessageKey, conflictEx.Args);
                break;

            case UnauthorizedException unauthorizedEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Success = false;
                response.MessageCode = unauthorizedEx.MessageKey;
                response.Message = messageService.GetMessage(unauthorizedEx.MessageKey);
                break;

            case ForbiddenException forbiddenEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Success = false;
                response.MessageCode = forbiddenEx.MessageKey;
                response.Message = messageService.GetMessage(forbiddenEx.MessageKey);
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Success = false;
                response.MessageCode = MessageKeys.InternalServerError;
                response.Message = messageService.GetMessage(MessageKeys.InternalServerError);

                // only show error in Development
                if (_env.IsDevelopment())
                {
                    response.Errors = new
                    {
                        exception = exception.GetType().Name,
                        message = exception.Message,
                        stackTrace = exception.StackTrace
                    };
                }
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}