using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers.Subscription
{
    [Route("api/subscriptions")]
    [Authorize]
    public class SubscriptionController : BaseApiController
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            IMessageService messageService,
            ILogger<SubscriptionController> logger)
            : base(messageService, logger)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var profileId = User.GetRequiredUserId();
            var current = await _subscriptionService.GetCurrentSubscriptionAsync(profileId);
            return Ok(current, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateCheckoutRequest request)
        {
            var profileId = User.GetRequiredUserId();
            var result = await _subscriptionService.CreateCheckoutSessionAsync(profileId, request.SubscriptionPlanId, request.Platform, request.Quantity);
            return Ok(result, MessageKeys.DataCreatedSuccessfully);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var profileId = User.GetRequiredUserId();
            var transactions = await _subscriptionService.GetTransactionsAsync(profileId, page, pageSize);
            return Ok(transactions, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpDelete("access-grants/{memberProfileId:guid}")]
        public async Task<IActionResult> RevokeAccessGrant(Guid memberProfileId)
        {
            var ownerProfileId = User.GetRequiredUserId();
            await _subscriptionService.RevokeAccessGrantAsync(ownerProfileId, memberProfileId);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        [HttpGet("payment/redirect/success")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentSuccess([FromQuery(Name = "session_id")] string? sessionId)
        {
            var url = await _subscriptionService.GetPaymentRedirectUrlAsync(sessionId, isSuccess: true);
            if (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { redirectUrl = url }, MessageKeys.DataRetrievedSuccessfully);
            }
            return Redirect(url);
        }

        [HttpGet("payment/redirect/cancel")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCancel([FromQuery(Name = "session_id")] string? sessionId)
        {
            var url = await _subscriptionService.GetPaymentRedirectUrlAsync(sessionId, isSuccess: false);
            if (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { redirectUrl = url }, MessageKeys.DataRetrievedSuccessfully);
            }
            return Redirect(url);
        }
    }
}
