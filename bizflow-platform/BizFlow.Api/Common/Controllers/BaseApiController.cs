using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Common.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly IMessageService MessageService;
        protected readonly ILogger Logger;

        protected BaseApiController(IMessageService messageService, ILogger logger)
        {
            MessageService = messageService;
            Logger = logger;
        }

        #region Success Responses

        /// <summary>
        /// 200 OK with data
        /// </summary>
        protected IActionResult Ok<T>(T data, string messageKey)
        {
            var response = ApiResponse<T>.SuccessResponse(
                data,
                messageKey,
                MessageService.GetMessage(messageKey)
            );
            return base.Ok(response);
        }

        /// <summary>
        /// 200 OK with data, params and message
        /// </summary>
        protected IActionResult Ok<T>(T data, string messageKey, params object[] args)
        {
            var response = ApiResponse<T>.SuccessResponse(
                data,
                messageKey,
                MessageService.GetMessage(messageKey, args)
            );
            return base.Ok(response);
        }

        /// <summary>
        /// 200 OK without data
        /// </summary>
        protected IActionResult Ok(string messageKey)
        {
            var response = ApiResponse.SuccessResponse(
                messageKey,
                MessageService.GetMessage(messageKey)
            );
            return base.Ok(response);
        }

        /// <summary>
        /// 201 Created
        /// </summary>
        protected IActionResult Created<T>(T data, string messageKey, string actionName, object routeValues)
        {
            var response = ApiResponse<T>.SuccessResponse(
                data,
                messageKey,
                MessageService.GetMessage(messageKey)
            );
            return CreatedAtAction(actionName, routeValues, response);
        }

        /// <summary>
        /// 204 No Content
        /// </summary>
        protected new IActionResult NoContent()
        {
            return base.NoContent();
        }

        #endregion

        #region Error Responses

        /// <summary>
        /// 400 Bad Request
        /// </summary>
        protected IActionResult BadRequest(string messageKey, object? errors = null)
        {
            var response = ApiResponse.ErrorResponse(
                messageKey,
                MessageService.GetMessage(messageKey),
                errors
            );
            return base.BadRequest(response);
        }

        /// <summary>
        /// 400 Bad Request with params
        /// </summary>
        protected IActionResult BadRequest(string messageKey, object? errors, params object[] args)
        {
            var response = ApiResponse.ErrorResponse(
                messageKey,
                MessageService.GetMessage(messageKey, args),
                errors
            );
            return base.BadRequest(response);
        }

        /// <summary>
        /// 401 Unauthorized
        /// </summary>
        protected IActionResult Unauthorized(string messageKey = MessageKeys.Unauthorized)
        {
            var response = ApiResponse.ErrorResponse(
                messageKey,
                MessageService.GetMessage(messageKey)
            );
            return base.Unauthorized(response);
        }

        /// <summary>
        /// 403 Forbidden
        /// </summary>
        protected IActionResult Forbidden(string messageKey = MessageKeys.Forbidden)
        {
            var response = ApiResponse.ErrorResponse(
                messageKey,
                MessageService.GetMessage(messageKey)
            );
            return StatusCode(StatusCodes.Status403Forbidden, response);
        }

        /// <summary>
        /// 404 Not Found
        /// </summary>
        protected IActionResult NotFound(string messageKey)
        {
            var response = ApiResponse.ErrorResponse(
                messageKey,
                MessageService.GetMessage(messageKey)
            );
            return base.NotFound(response);
        }

        /// <summary>
        /// 404 Not Found with params
        /// </summary>
        protected IActionResult NotFound(string messageKey, params object[] args)
        {
            var response = ApiResponse.ErrorResponse(
                messageKey,
                MessageService.GetMessage(messageKey, args)
            );
            return base.NotFound(response);
        }

        /// <summary>
        /// 409 Conflict
        /// </summary>
        protected IActionResult Conflict(string messageKey, params object[] args)
        {
            var response = ApiResponse.ErrorResponse(
                messageKey,
                MessageService.GetMessage(messageKey, args)
            );
            return base.Conflict(response);
        }

        /// <summary>
        /// 500 Internal Server Error
        /// </summary>
        protected IActionResult InternalServerError(Exception ex, string messageKey = MessageKeys.InternalServerError)
        {
            Logger.LogError(ex, "Internal Server Error: {Message}", ex.Message);

            var response = ApiResponse.ErrorResponse(
                messageKey,
                MessageService.GetMessage(messageKey)
            );
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }

        #endregion

        #region Pagination Response

        /// <summary>
        /// response with pagination
        /// </summary>
        protected IActionResult OkPaginated<T>(PaginatedResponse<T> paginatedData, string messageKey)
        {
            var response = ApiResponse<PaginatedResponse<T>>.SuccessResponse(
                paginatedData,
                messageKey,
                MessageService.GetMessage(messageKey)
            );
            return base.Ok(response);
        }

        #endregion
    }
}
