using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Revenue
{
    [Route("api/my-business/accounting")]
    public class RevenueController : PaginatedApiController
    {
        private readonly IRevenueService _revenueService;

        private static readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

        public RevenueController(
            IRevenueService revenueService,
            IMessageService messageService,
            IOptions<PaginationSettings> paginationSettings,
            ILogger<RevenueController> logger)
            : base(messageService, logger, paginationSettings)
        {
            _revenueService = revenueService;
        }

        [HttpPost("revenues/manual")]
        [SwaggerOperation(Summary = "Create manual revenue")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateManual([FromBody] CreateManualRevenueRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(MessageKeys.ValidationError, ModelState);

            var userId = GetCurrentUserId();
            var result = await _revenueService.CreateManualAsync(userId, request);
            return Created(result, MessageKeys.DataCreatedSuccessfully, nameof(GetRevenues), new { });
        }

        [HttpGet("revenues")]
        [SwaggerOperation(Summary = "List revenues")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRevenues([FromQuery] RevenueQueryParams query)
        {
            ApplyPaginationDefaults(query);

            var userId = GetCurrentUserId();
            var result = await _revenueService.ListAsync(userId, query);
            return OkPaginated(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpDelete("revenues/{revenueId:long}")]
        [SwaggerOperation(Summary = "Delete manual revenue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteManual(long revenueId)
        {
            var userId = GetCurrentUserId();
            await _revenueService.DeleteManualAsync(userId, revenueId);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        private Guid GetCurrentUserId()
        {
            return _mockCurrentUserId;
        }
    }
}
