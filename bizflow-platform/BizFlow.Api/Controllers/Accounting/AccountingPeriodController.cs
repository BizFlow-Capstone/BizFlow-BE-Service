using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Accounting;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Accounting;

[Authorize]
[Route("api/locations/{locationId:int}/accounting/periods")]
public class AccountingPeriodController : BaseApiController
{
    private readonly IAccountingPeriodService _accountingPeriodService;

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

    [HttpPost("custom")]
    [SwaggerOperation(Summary = "Create custom accounting period", Description = "Create accounting period by custom start and end date")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCustomPeriod(int locationId, [FromBody] CreateCustomAccountingPeriodRequest request)
    {
        var period = await _accountingPeriodService.CreateCustomPeriodAsync(locationId, GetCurrentUserId(), request);
        return Created(period, MessageKeys.PeriodCreatedSuccessfully, nameof(GetPeriodDetail), new { locationId, periodId = period.PeriodId });
    }

    [HttpPost("opening-balance-suggestion")]
    [SwaggerOperation(Summary = "Get opening balance suggestion", Description = "Suggest carry opening cash and bank balances for period creation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOpeningBalanceSuggestion(int locationId, [FromBody] OpeningBalanceSuggestionRequest request)
    {
        var suggestion = await _accountingPeriodService.GetOpeningBalanceSuggestionAsync(locationId, GetCurrentUserId(), request);

        suggestion.SuggestionReason = MessageService.GetMessage(suggestion.SuggestionReasonCode);
        if (!string.IsNullOrWhiteSpace(suggestion.CalculationExplanationCode))
        {
            suggestion.CalculationExplanation = MessageService.GetMessage(suggestion.CalculationExplanationCode);
        }

        return Ok(suggestion, MessageKeys.DataRetrievedSuccessfully);
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

    [HttpDelete("{periodId:long}")]
    [SwaggerOperation(Summary = "Delete accounting period", Description = "Delete an open period with no books and no tax payments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePeriod(int locationId, long periodId)
    {
        await _accountingPeriodService.DeletePeriodAsync(locationId, periodId, GetCurrentUserId());
        return Ok(MessageKeys.DataDeletedSuccessfully);
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

    private Guid GetCurrentUserId() => User.GetRequiredUserId();
}