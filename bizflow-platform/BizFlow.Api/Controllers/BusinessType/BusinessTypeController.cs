using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.BusinessType
{
    /// <summary>
    /// Business Type Management APIs
    /// </summary>
    [Route("api/business-types")]
    [Authorize]
    public class BusinessTypeController : BaseApiController
    {
        private readonly IBusinessTypeService _businessTypeService;

        public BusinessTypeController(
            IBusinessTypeService businessTypeService,
            IMessageService messageService,
            ILogger<BusinessTypeController> logger)
            : base(messageService, logger)
        {
            _businessTypeService = businessTypeService;
        }

        /// <summary>
        /// Get active business type by id
        /// </summary>
        [HttpGet("{businessTypeId:guid}")]
        [SwaggerOperation(Summary = "Get active business type by id", Description = "Returns one active business type by id.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActiveById(Guid businessTypeId)
        {
            try
            {
                var businessType = await _businessTypeService.GetActiveByIdAsync(businessTypeId);
                return Ok(businessType, MessageKeys.DataRetrievedSuccessfully);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(MessageKeys.NotFound);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
