using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Import;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Import
{
    /// <summary>
    /// Import Management APIs
    /// </summary>
    [Route("api/my-business/accounting")]
    public class ImportController : PaginatedApiController
    {
        private readonly IImportService _importService;

        public ImportController(
            IImportService importService,
            IMessageService messageService,
            IOptions<PaginationSettings> paginationSettings,
            ILogger<ImportController> logger)
            : base(messageService, logger, paginationSettings)
        {
            _importService = importService;
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
            var result = await _importService.GetTemplateAsync();
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Create a new import (DRAFT)
        /// </summary>
        [HttpPost("import")]
        [SwaggerOperation(Summary = "Create import", Description = "Creates a new DRAFT import with items. Upload image via multipart/form-data. Items as JSON string. TotalAmount is server-calculated.")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateImport([FromForm] CreateImportRequest request, IFormFile? image)
        {
            if (image != null)
            {
                request.ImageStream = image.OpenReadStream();
                request.ImageFileName = image.FileName;
            }

            var userId = GetCurrentUserId();
            var result = await _importService.CreateImportAsync(userId, request);
            return Created(result, MessageKeys.DataCreatedSuccessfully, nameof(GetImportDetail), new { importId = result.ImportId });
        }

        /// <summary>
        /// Update a DRAFT import
        /// </summary>
        [HttpPut("import/{importId:long}")]
        [SwaggerOperation(Summary = "Update import", Description = "Updates a DRAFT import. Upload image via multipart/form-data. Set RemoveImage=true to remove image. Providing items replaces all existing items.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateImport(long importId, [FromForm] UpdateImportRequest request, IFormFile? image)
        {
            if (image != null)
            {
                request.ImageStream = image.OpenReadStream();
                request.ImageFileName = image.FileName;
            }

            var userId = GetCurrentUserId();
            var result = await _importService.UpdateImportAsync(userId, importId, request);
            return Ok(result, MessageKeys.DataUpdatedSuccessfully);
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
            var userId = GetCurrentUserId();
            var result = await _importService.PatchImportAsync(userId, importId, request);

            return Ok(result, MessageKeys.ImportConfirmedSuccessfully);
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
            var userId = GetCurrentUserId();
            var result = await _importService.GetImportDetailAsync(userId, importId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
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
            var userId = GetCurrentUserId();
            await _importService.DeleteImportAsync(userId, importId);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        #region Private Helper Methods

        private Guid GetCurrentUserId() => User.GetRequiredUserId();

        #endregion
    }
}
