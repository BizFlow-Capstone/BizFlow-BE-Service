using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Api.Common.Filters;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Employee;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Employee
{
    [Authorize]
    [Route("api/my-employee")]
    public class EmployeeController : BaseApiController
    {
        private readonly IEmployeeService _employeeService;
        private readonly INotificationService _notificationService;

        public EmployeeController(
            IEmployeeService employeeService,
            INotificationService notificationService,
            IMessageService messageService,
            ILogger<EmployeeController> logger)
            : base(messageService, logger)
        {
            _employeeService = employeeService;
            _notificationService = notificationService;
        }

        [HttpGet("search")]
        [SwaggerOperation(Summary = "Search employees by phone/email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchEmployees([FromQuery] string query)
        {
            var userId = GetCurrentUserId();
            var result = await _employeeService.SearchUserByContactAsync(userId, query);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("invite")]
        [RequireFeature(FeatureCodes.Employees, useOwnerScope: true)]
        [SwaggerOperation(Summary = "Invite employee")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> InviteEmployee([FromBody] InviteEmployeeRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _employeeService.InviteEmployeeAsync(userId, request.EmployeeId);
            return Ok(result, MessageKeys.EmployeeInviteSuccess);
        }

        [HttpDelete("{employeeId:guid}")]
        [SwaggerOperation(Summary = "Remove employee")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveEmployee(Guid employeeId)
        {
            var userId = GetCurrentUserId();
            await _employeeService.RemoveEmployeeAsync(userId, employeeId);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        [HttpGet("invitations")]
        [SwaggerOperation(Summary = "Get my pending invitations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingInvitations()
        {
            var userId = GetCurrentUserId();
            var result = await _employeeService.GetPendingInvitationsAsync(userId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("invitations/{hireId:int}/accept")]
        [SwaggerOperation(Summary = "Accept invitation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcceptInvitation(int hireId)
        {
            var userId = GetCurrentUserId();
            await _employeeService.AcceptInvitationAsync(userId, hireId);
            return Ok(MessageKeys.DataUpdatedSuccessfully);
        }

        [HttpPost("invitations/{hireId:int}/reject")]
        [SwaggerOperation(Summary = "Reject invitation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectInvitation(int hireId)
        {
            var userId = GetCurrentUserId();
            await _employeeService.RejectInvitationAsync(userId, hireId);
            return Ok(MessageKeys.DataUpdatedSuccessfully);
        }

        [HttpPost("invitations/reply-notification")]
        [SwaggerOperation(
            Summary = "Send invitation reply notification",
            Description = "Notify the business owner that the employee has accepted or rejected the invitation. Uses the INVITE_ACCEPTED / INVITE_REJECTED notification templates stored in the database.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendInvitationReplyNotification([FromBody] SendInvitationReplyNotificationRequest request)
        {
            await _notificationService.SendInvitationReplyAsync(
                request.OwnerUserId,
                request.IsAccepted,
                request.EmployeeName);
            return Ok(MessageKeys.DataUpdatedSuccessfully);
        }

        private Guid GetCurrentUserId() => User.GetRequiredUserId();
    }
}
