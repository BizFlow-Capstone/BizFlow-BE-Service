using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.AccountingBook;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Accounting;

[Authorize]
[Route("api/locations/{locationId:int}/accounting/books")]
public class AccountingBookController : BaseApiController
{
    private readonly IAccountingBookService _accountingBookService;

    public AccountingBookController(
        IAccountingBookService accountingBookService,
        IMessageService messageService,
        ILogger<AccountingBookController> logger)
        : base(messageService, logger)
    {
        _accountingBookService = accountingBookService;
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create accounting books", Description = "Create accounting books from templateCodes for a period")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBooks(int locationId, [FromBody] CreateBooksRequest request)
    {
        var result = await _accountingBookService.CreateBooksAsync(locationId, GetCurrentUserId(), request);
        return Created(result, MessageKeys.DataCreatedSuccessfully, nameof(GetBookSummary),
            new { locationId, bookId = result.CreatedBooks.FirstOrDefault()?.BookId ?? 0 });
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List accounting books", Description = "List all accounting books for a location and period")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListBooks(int locationId, [FromQuery] long periodId)
    {
        var result = await _accountingBookService.ListBooksAsync(locationId, GetCurrentUserId(), periodId);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpGet("{bookId:long}/summary")]
    [SwaggerOperation(Summary = "Get book summary", Description = "Get KPI summary for an accounting book (fast)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookSummary(int locationId, long bookId)
    {
        var result = await _accountingBookService.GetBookSummaryAsync(locationId, GetCurrentUserId(), bookId);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpGet("{bookId:long}/rows")]
    [SwaggerOperation(Summary = "Get book rows", Description = "Get accounting book data rows with cursor-based pagination")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookRows(
        int locationId, long bookId,
        [FromQuery] string? cursor,
        [FromQuery] int batchSize = 200)
    {
        if (batchSize > 200) batchSize = 200;
        var result = await _accountingBookService.GetBookRowsAsync(locationId, GetCurrentUserId(), bookId, cursor, batchSize);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    private Guid GetCurrentUserId() => User.GetRequiredUserId();
}
