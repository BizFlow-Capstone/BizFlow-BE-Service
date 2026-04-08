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

        [HttpGet]
        [SwaggerOperation(Summary = "Get my notifications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyNotifications([FromQuery] NotificationQueryParams query)
        {
            var userId = User.GetRequiredUserId();
            var result = await _notificationService.GetUserNotificationsAsync(userId, query);
            return OkPaginated(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("{userNotificationId:long}")]
        [SwaggerOperation(Summary = "Get my notification detail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyNotificationDetail(long userNotificationId)
        {
            var userId = User.GetRequiredUserId();
            var notification = await _notificationService.GetUserNotificationDetailAsync(userId, userNotificationId);
            if (notification == null)
            {
                return NotFound(MessageKeys.NotFound);
            }

            return Ok(notification, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("unread-count")]
        [SwaggerOperation(Summary = "Get unread notification count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.GetRequiredUserId();
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new NotificationUnreadCountDto { UnreadCount = unreadCount }, MessageKeys.NotificationUnreadCountRetrieved);
        }

        [HttpPut("{userNotificationId:long}/read")]
        [SwaggerOperation(Summary = "Mark one notification as read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsRead(long userNotificationId)
        {
            var userId = User.GetRequiredUserId();
            var updated = await _notificationService.MarkAsReadAsync(userId, userNotificationId);
            if (!updated)
            {
                return NotFound(MessageKeys.NotFound);
            }

            return Ok(MessageKeys.NotificationMarkedAsRead);
        }

        [HttpPut("read-all")]
        [SwaggerOperation(Summary = "Mark all notifications as read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.GetRequiredUserId();
            var updatedCount = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { updatedCount }, MessageKeys.NotificationMarkedAllAsRead);
        }
    }
}
