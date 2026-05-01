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
[Route("api/my-business/accounting/document-numbers")]
public class DocumentNumberController : BaseApiController
{
    private readonly IDocumentNumberRegistryService _documentNumberRegistryService;

    public DocumentNumberController(
        IDocumentNumberRegistryService documentNumberRegistryService,
        IMessageService messageService,
        ILogger<DocumentNumberController> logger)
        : base(messageService, logger)
    {
        _documentNumberRegistryService = documentNumberRegistryService;
    }

    [HttpGet("exists")]
    [SwaggerOperation(Summary = "Check document number existence")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Exists(
        [FromQuery] CheckDocumentNumberExistsQuery query,
        CancellationToken cancellationToken
    )
    {
        var exists = await _documentNumberRegistryService.ExistsAsync(
            User.GetRequiredUserId(),
            query.DocumentNumber,
            query.ExcludeCostId,
            query.ExcludeRevenueId,
            cancellationToken);

        return Ok(
            new DocumentNumberExistsResponseDto { Exists = exists },
            MessageKeys.DataRetrievedSuccessfully
        );
    }
}
