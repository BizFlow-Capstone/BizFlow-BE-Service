using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Cost;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using System.IO;

namespace BizFlow.Api.Controllers.Cost
{
    /// <summary>
    /// Cost management APIs.
    /// </summary>
    [Route("api/my-business/accounting")]
    public class CostController : PaginatedApiController
    {
        private readonly ICostService _costService;

        // TODO: Replace with actual JWT-based user identification
        private static readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

        public CostController(
            ICostService costService,
            IMessageService messageService,
            IOptions<PaginationSettings> paginationSettings,
            ILogger<CostController> logger)
            : base(messageService, logger, paginationSettings)
        {
            _costService = costService;
        }

        /// <summary>
        /// Creates a manual cost record.
        /// </summary>
        [HttpPost("costs/manual")]
        [SwaggerOperation(Summary = "Create manual cost")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateManual([FromForm] CreateManualCostRequest request, IFormFile? image)
        {
            MemoryStream? uploadedImageStream = null;
            if (image != null)
            {
                uploadedImageStream = new MemoryStream();
                await image.CopyToAsync(uploadedImageStream);
                uploadedImageStream.Position = 0;
                request.ImageStream = uploadedImageStream;
                request.ImageFileName = image.FileName;
            }

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(MessageKeys.ValidationError, ModelState);

                var userId = GetCurrentUserId();
                var result = await _costService.CreateManualAsync(userId, request);
                return Created(result, MessageKeys.DataCreatedSuccessfully, nameof(GetCosts), new { });
            }
            finally
            {
                uploadedImageStream?.Dispose();
            }
        }

        /// <summary>
        /// Updates a manual cost record (fields: description, amount, costDate, paymentMethod, documents).
        /// </summary>
        [HttpPut("costs/{costId:long}")]
        [SwaggerOperation(Summary = "Update manual cost")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateManual(long costId, [FromForm] UpdateManualCostRequest request, IFormFile? image)
        {
            MemoryStream? uploadedImageStream = null;
            if (image != null)
            {
                uploadedImageStream = new MemoryStream();
                await image.CopyToAsync(uploadedImageStream);
                uploadedImageStream.Position = 0;
                request.ImageStream = uploadedImageStream;
                request.ImageFileName = image.FileName;
            }

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(MessageKeys.ValidationError, ModelState);

                var userId = GetCurrentUserId();
                var result = await _costService.UpdateManualAsync(userId, costId, request);
                return Ok(result, MessageKeys.DataUpdatedSuccessfully);
            }
            finally
            {
                uploadedImageStream?.Dispose();
            }
        }

        /// <summary>
        /// Returns paginated costs with optional filters.
        /// </summary>
        [HttpGet("costs")]
        [SwaggerOperation(Summary = "List costs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCosts([FromQuery] CostQueryParams query)
        {
            ApplyPaginationDefaults(query);

            var userId = GetCurrentUserId();
            var result = await _costService.ListAsync(userId, query);
            return OkPaginated(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Soft-deletes a manual cost record.
        /// </summary>
        [HttpDelete("costs/{costId:long}")]
        [SwaggerOperation(Summary = "Delete manual cost")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteManual(long costId)
        {
            var userId = GetCurrentUserId();
            await _costService.DeleteManualAsync(userId, costId);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        #region Private Helper Methods

        private Guid GetCurrentUserId()
        {
            return _mockCurrentUserId;
        }

        #endregion
    }
}
