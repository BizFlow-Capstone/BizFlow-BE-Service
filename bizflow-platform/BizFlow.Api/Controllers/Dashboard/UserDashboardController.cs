using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Dashboard;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Dashboard;

/// <summary>
/// User dashboard aggregates (revenue, cost, orders, outstanding debt).
/// </summary>
[Route("api/my-business/dashboard")]
[Authorize]
public class UserDashboardController : BaseApiController
{
    private readonly IUserDashboardService _dashboardService;

    public UserDashboardController(
        IUserDashboardService dashboardService,
        IMessageService messageService,
        ILogger<UserDashboardController> logger)
        : base(messageService, logger)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Summary KPIs. Omit businessLocationId to aggregate across all locations the user can access; pass businessLocationId to scope to one location.
    /// Presets: day = that calendar day; week = Mon–Sun containing referenceDate; month = 1st–last of that month; year = Jan 1–Dec 31 of that year (referenceDate defaults to UTC today). Custom uses fromDate/toDate from the query.
    /// Outstanding debt is a current snapshot (not limited to the range).
    /// </summary>
    [HttpGet("summary")]
    [SwaggerOperation(Summary = "Dashboard summary (all accessible locations or one location)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary([FromQuery] DashboardSummaryQuery query, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(MessageKeys.ValidationError, ModelState);

        var result = await _dashboardService.GetSummaryAsync(GetCurrentUserId(), query, cancellationToken);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    private Guid GetCurrentUserId() => User.GetRequiredUserId();
}
