using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Accounting;

[Route("api/locations/{locationId:int}/accounting/periods")]
public class AccountingPeriodController : BaseApiController
{
    private readonly IAccountingPeriodService _accountingPeriodService;

    // TODO: Replace with JWT claims extraction for production.
    private static readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

    public AccountingPeriodController(
        IAccountingPeriodService accountingPeriodService,
        IMessageService messageService,
        ILogger<AccountingPeriodController> logger)
        : base(messageService, logger)
    {
        _accountingPeriodService = accountingPeriodService;
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create accounting period", Description = "Create accounting period for a location")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePeriod(int locationId, [FromBody] CreateAccountingPeriodRequest request)
    {
        var period = await _accountingPeriodService.CreatePeriodAsync(locationId, GetCurrentUserId(), request);
        return Created(period, MessageKeys.PeriodCreatedSuccessfully, nameof(GetPeriodDetail), new { locationId, periodId = period.PeriodId });
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List accounting periods", Description = "List all accounting periods by location")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPeriods(int locationId)
    {
        var periods = await _accountingPeriodService.GetPeriodsAsync(locationId, GetCurrentUserId());
        return Ok(periods, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpGet("{periodId:long}")]
    [SwaggerOperation(Summary = "Get accounting period detail", Description = "Get accounting period detail by id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPeriodDetail(int locationId, long periodId)
    {
        var period = await _accountingPeriodService.GetPeriodDetailAsync(locationId, periodId, GetCurrentUserId());
        return Ok(period, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpPost("{periodId:long}/finalize")]
    [SwaggerOperation(Summary = "Finalize period", Description = "Finalize an accounting period")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FinalizePeriod(int locationId, long periodId)
    {
        var period = await _accountingPeriodService.FinalizePeriodAsync(locationId, periodId, GetCurrentUserId());
        return Ok(period, MessageKeys.PeriodFinalizedSuccessfully);
    }

    [HttpPost("{periodId:long}/reopen")]
    [SwaggerOperation(Summary = "Reopen period", Description = "Reopen a finalized accounting period")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReopenPeriod(int locationId, long periodId, [FromBody] ReopenAccountingPeriodRequest request)
    {
        var period = await _accountingPeriodService.ReopenPeriodAsync(locationId, periodId, GetCurrentUserId(), request.Reason);
        return Ok(period, MessageKeys.PeriodReopenedSuccessfully);
    }

    [HttpGet("{periodId:long}/audit-logs")]
    [SwaggerOperation(Summary = "Get period audit logs", Description = "Get all audit logs for a period")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLogs(int locationId, long periodId)
    {
        var logs = await _accountingPeriodService.GetAuditLogsAsync(locationId, periodId, GetCurrentUserId());
        return Ok(logs, MessageKeys.DataRetrievedSuccessfully);
    }

    private static Guid GetCurrentUserId() => _mockCurrentUserId;
}