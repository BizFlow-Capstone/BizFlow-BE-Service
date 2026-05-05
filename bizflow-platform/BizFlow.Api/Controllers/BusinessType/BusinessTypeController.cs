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
            ILogger<BusinessTypeController> logger
        )
            : base(messageService, logger)
        {
            _businessTypeService = businessTypeService;
        }

        /// <summary>
        /// Get all active business types
        /// </summary>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get active business types",
            Description = "Returns all active business types."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllActive()
        {
            var businessTypes = await _businessTypeService.GetAllActiveAsync();
            return Ok(businessTypes, MessageKeys.DataRetrievedSuccessfully);
        }
    }
}
