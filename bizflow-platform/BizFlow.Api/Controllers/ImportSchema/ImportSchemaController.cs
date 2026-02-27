using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.ImportSchema;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.ImportSchema
{
    /// <summary>
    /// Import Schema Management APIs
    /// </summary>
    [Route("api/admin")]
    public class ImportSchemaController : BaseApiController
    {
        private readonly IImportSchemaService _importSchemaService;

        public ImportSchemaController(
            IImportSchemaService importSchemaService,
            IMessageService messageService,
            ILogger<ImportSchemaController> logger)
            : base(messageService, logger)
        {
            _importSchemaService = importSchemaService;
        }

        /// <summary>
        /// Get all import schemas
        /// </summary>
        [HttpGet("import-schemas")]
        [SwaggerOperation(Summary = "List import schemas", Description = "Returns all import schemas (non-soft-deleted) with their active version.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _importSchemaService.GetAllAsync();
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Get import schema by ID
        /// </summary>
        [HttpGet("import-schema/{id:int}")]
        [SwaggerOperation(Summary = "Get import schema detail", Description = "Returns import schema with its active version.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _importSchemaService.GetByIdAsync(id);
                return Ok(result, MessageKeys.DataRetrievedSuccessfully);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
        }

        /// <summary>
        /// Create a new import schema with its first version
        /// </summary>
        [HttpPost("import-schema")]
        [SwaggerOperation(Summary = "Create import schema", Description = "Creates a new import schema with its first version (SchemaJson required). Schema is created as inactive.")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateImportSchemaRequest request)
        {
            try
            {
                var result = await _importSchemaService.CreateAsync(request);
                return Created(result, MessageKeys.DataCreatedSuccessfully, nameof(GetById), new { id = result.ImportSchemaId });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.MessageKey);
            }
        }

        /// <summary>
        /// Update an import schema; creates a new version only if SchemaJson changed
        /// </summary>
        [HttpPut("import-schema/{id:int}")]
        [SwaggerOperation(Summary = "Update import schema", Description = "Updates schema fields. If SchemaJson is provided and different from current active version, a new version is created (active), old version becomes inactive.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateImportSchemaRequest request)
        {
            try
            {
                var result = await _importSchemaService.UpdateAsync(id, request);
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
        /// Activate a schema (deactivates all others)
        /// </summary>
        [HttpPatch("import-schema/{id:int}/activate")]
        [SwaggerOperation(Summary = "Activate import schema", Description = "Deactivates all schemas first, then activates the target schema and sets EverActivated = true.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                await _importSchemaService.ActivateAsync(id);
                return Ok(MessageKeys.DataUpdatedSuccessfully);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.MessageKey, ex.Args);
            }
        }

        /// <summary>
        /// Delete an import schema (smart: soft/hard based on EverActivated)
        /// </summary>
        [HttpDelete("import-schema/{id:int}")]
        [SwaggerOperation(Summary = "Delete import schema", Description = "Cannot delete active schema (400). Ever-activated → soft delete (DeletedAt set). Never-activated → hard delete.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _importSchemaService.DeleteAsync(id);
                return Ok(MessageKeys.DataDeletedSuccessfully);
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
    }
}
