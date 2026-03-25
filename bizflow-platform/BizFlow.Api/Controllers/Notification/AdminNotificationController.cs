using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Notification;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Notification
{
    [Authorize]
    [Route("api/admin/notifications")]
    public class AdminNotificationController : BaseApiController
    {
        private readonly INotificationService _notificationService;

        public AdminNotificationController(
            INotificationService notificationService,
            IMessageService messageService,
            ILogger<AdminNotificationController> logger)
            : base(messageService, logger)
        {
            _notificationService = notificationService;
        }

        [HttpGet("templates")]
        [SwaggerOperation(Summary = "Get notification templates")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplates()
        {
            var result = await _notificationService.GetTemplatesAsync();
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("action-catalog")]
        [SwaggerOperation(Summary = "Get notification action/target catalog")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActionCatalog()
        {
            var result = await _notificationService.GetActionCatalogAsync();
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("templates/{eventCode}")]
        [SwaggerOperation(Summary = "Get notification template by event code")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplateByEventCode(string eventCode)
        {
            var result = await _notificationService.GetTemplateByEventCodeAsync(eventCode);
            if (result == null)
            {
                return NotFound(MessageKeys.NotFound);
            }

            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPut("templates/{eventCode}")]
        [Authorize]
        [SwaggerOperation(Summary = "Create/update notification template")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertTemplate(string eventCode, [FromBody] UpsertNotificationTemplateRequest request)
        {
            try
            {
                var result = await _notificationService.UpsertTemplateAsync(eventCode, request);
                return Ok(result, MessageKeys.NotificationTemplateSaved);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.MessageKey, ex.Errors, ex.Args);
            }
        }

        [HttpPatch("templates/{eventCode}/toggle")]
        [Authorize]
        [SwaggerOperation(Summary = "Toggle notification template active status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleTemplate(string eventCode, [FromBody] ToggleNotificationTemplateRequest request)
        {
            try
            {
                var result = await _notificationService.ToggleTemplateAsync(eventCode, request.IsActive);
                return Ok(result, MessageKeys.NotificationTemplateUpdated);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
        }

        [HttpPost("dispatches")]
        [Authorize]
        [SwaggerOperation(Summary = "Create notification dispatch (send now or schedule)")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateDispatch([FromBody] CreateNotificationDispatchRequest request)
        {
            try
            {
                var createdByUserId = User.GetRequiredUserId();
                var result = await _notificationService.CreateDispatchAsync(createdByUserId, request);
                return Ok(result, MessageKeys.NotificationDispatchCreated);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.MessageKey, ex.Errors, ex.Args);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
        }

        [HttpGet("dispatches")]
        [SwaggerOperation(Summary = "List notification dispatches")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDispatches([FromQuery] NotificationDispatchQueryParams query)
        {
            var result = await _notificationService.GetDispatchesAsync(query);
            return OkPaginated(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("dispatches/process-due")]
        [Authorize]
        [SwaggerOperation(Summary = "Process due scheduled notifications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ProcessDueDispatches()
        {
            await _notificationService.ProcessDueDispatchesAsync();
            return Ok(MessageKeys.NotificationDispatchProcessed);
        }
    }
}