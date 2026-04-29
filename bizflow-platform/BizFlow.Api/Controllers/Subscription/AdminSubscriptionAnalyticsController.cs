using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Subscription;

[Route("api/admin/subscriptions")]
[Authorize]
public class AdminSubscriptionAnalyticsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;

    public AdminSubscriptionAnalyticsController(
        ISubscriptionService subscriptionService,
        IMessageService messageService,
        ILogger<AdminSubscriptionAnalyticsController> logger)
        : base(messageService, logger)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("analytics")]
    [SwaggerOperation(Summary = "Get subscription analytics (admin only)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAnalytics([FromQuery] AdminSubscriptionAnalyticsQuery query, CancellationToken cancellationToken)
    {
        User.EnsureAdminRole();

        if (!ModelState.IsValid)
            return BadRequest(MessageKeys.ValidationError, ModelState);

        var result = await _subscriptionService.GetAdminAnalyticsAsync(query, cancellationToken);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }
}
