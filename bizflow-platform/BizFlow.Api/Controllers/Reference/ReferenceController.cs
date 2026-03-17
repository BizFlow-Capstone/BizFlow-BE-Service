using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Reference
{
    /// <summary>
    /// Reference / lookup data
    /// </summary>
    [Route("api/reference")]
    public class ReferenceController : BaseApiController
    {
        public ReferenceController(IMessageService messageService, ILogger<ReferenceController> logger)
            : base(messageService, logger) { }

        /// <summary>
        /// Get all supported payment methods
        /// </summary>
        [HttpGet("payment-methods")]
        [SwaggerOperation(Summary = "Get payment methods", Description = "Returns all supported payment method codes (e.g. cash, bank).")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetPaymentMethods()
        {
            return Ok(PaymentMethods.All, MessageKeys.DataRetrievedSuccessfully);
        }
    }
}
