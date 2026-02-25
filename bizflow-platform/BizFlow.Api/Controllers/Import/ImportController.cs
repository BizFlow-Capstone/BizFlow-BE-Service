using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Import;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Import
{
    /// <summary>
    /// Import Management APIs
    /// </summary>
    [Route("api/my-business/accounting")]
    public class ImportController : BaseApiController
    {
        private readonly IImportService _importService;
        private readonly PaginationSettings _paginationSettings;

        // TODO: Replace with actual JWT-based user identification
        private static readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

        public ImportController(
            IImportService importService,
            IMessageService messageService,
            IOptions<PaginationSettings> paginationSettings,
            ILogger<ImportController> logger)
            : base(messageService, logger)
        {
            _importService = importService;
            _paginationSettings = paginationSettings.Value;
        }

        /// <summary>
        /// Get active import JSON schema template
        /// </summary>
        [HttpGet("import-template")]
        [SwaggerOperation(Summary = "Get import template", Description = "Returns the active JSON schema for import validation.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplate()
        {
            try
            {
                var result = await _importService.GetTemplateAsync();
                return Ok(result, MessageKeys.DataRetrievedSuccessfully);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
        }

        /// <summary>
        /// Create a new import (DRAFT)
        /// </summary>
        [HttpPost("import")]
        [SwaggerOperation(Summary = "Create import", Description = "Creates a new DRAFT import with items. TotalAmount is server-calculated.")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateImport([FromBody] CreateImportRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _importService.CreateImportAsync(userId, request);
                return Created(result, MessageKeys.DataCreatedSuccessfully, nameof(GetImportDetail), new { importId = result.ImportId });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.MessageKey);
            }
        }

        /// <summary>
        /// Update a DRAFT import
        /// </summary>
        [HttpPut("import/{importId:long}")]
        [SwaggerOperation(Summary = "Update import", Description = "Updates a DRAFT import. Providing items replaces all existing items.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateImport(long importId, [FromBody] UpdateImportRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _importService.UpdateImportAsync(userId, importId, request);
                return Ok(result, MessageKeys.DataUpdatedSuccessfully);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.MessageKey);
            }
        }

        /// <summary>
        /// Confirm or cancel an import
        /// </summary>
        [HttpPatch("import/{importId:long}")]
        [SwaggerOperation(Summary = "Confirm a DRAFT import",
            Description = "Confirms a DRAFT import: updates stock for each item. ReceivedAt is required.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PatchImport(long importId, [FromBody] PatchImportRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _importService.PatchImportAsync(userId, importId, request);

                return Ok(result, MessageKeys.ImportConfirmedSuccessfully);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.MessageKey);
            }
        }

        /// <summary>
        /// List imports with filters and pagination
        /// </summary>
        [HttpGet("imports")]
        [SwaggerOperation(Summary = "List imports", Description = "Returns paginated list of imports. Filters: status, importType, businessLocationId, fromDate, toDate.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ListImports([FromQuery] ImportQueryParams query)
        {
            ApplyPaginationDefaults(query);

            var userId = GetCurrentUserId();
            var result = await _importService.ListImportsAsync(userId, query);
            return OkPaginated(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Get full import detail including items
        /// </summary>
        [HttpGet("import/{importId:long}")]
        [SwaggerOperation(Summary = "Get import detail", Description = "Returns full import detail including all product items.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetImportDetail(long importId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _importService.GetImportDetailAsync(userId, importId);
                return Ok(result, MessageKeys.DataRetrievedSuccessfully);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
        }

        /// <summary>
        /// Delete an import (hard delete; CONFIRMED reverses stock)
        /// </summary>
        [HttpDelete("import/{importId:long}")]
        [SwaggerOperation(Summary = "Delete import",
            Description = "DRAFT → hard deleted. CONFIRMED → soft cancelled (stock reversed, record kept). CANCELLED → 400 error.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteImport(long importId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _importService.DeleteImportAsync(userId, importId);
                return Ok(MessageKeys.DataDeletedSuccessfully);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
        }

        #region Private Helper Methods

        private Guid GetCurrentUserId()
        {
            return _mockCurrentUserId;
        }

        private void ApplyPaginationDefaults(PaginationParams pagination)
        {
            pagination.PageNumber ??= _paginationSettings.DefaultPageNumber;
            pagination.PageSize ??= _paginationSettings.DefaultPageSize;

            if (pagination.PageSize > _paginationSettings.MaxPageSize)
                pagination.PageSize = _paginationSettings.MaxPageSize;
        }

        #endregion
    }
}
