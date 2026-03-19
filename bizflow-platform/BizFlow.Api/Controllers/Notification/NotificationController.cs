using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Notification;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Notification
{
    [Authorize]
    [Route("api/notifications")]
    public class NotificationController : BaseApiController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(
            INotificationService notificationService,
            IMessageService messageService,
            ILogger<NotificationController> logger)
            : base(messageService, logger)
        {
            _notificationService = notificationService;
        }

        [HttpPost("register-device-token")]
        [SwaggerOperation(Summary = "Register device token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenRequest request)
        {
            var userId = User.GetRequiredUserId();
            await _notificationService.RegisterDeviceTokenAsync(userId, request.Token, request.DeviceName, request.Platform);
            return Ok(MessageKeys.DeviceTokenRegistered);
        }

        [HttpPost("unregister-device-token")]
        [SwaggerOperation(Summary = "Unregister device token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UnregisterDeviceToken([FromBody] UnregisterDeviceTokenRequest request)
        {
            var userId = User.GetRequiredUserId();
            await _notificationService.UnregisterDeviceTokenAsync(userId, request.Token);
            return Ok(MessageKeys.DataUpdatedSuccessfully);
        }
    }
}
