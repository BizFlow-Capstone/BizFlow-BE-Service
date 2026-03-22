using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Reference
{
    /// <summary>
    /// Reference / lookup data
    /// </summary>
    [Route("api/reference")]
    [Authorize]
    public class ReferenceController : BaseApiController
    {
        private readonly IReferenceService _referenceService;

        public ReferenceController(
            IReferenceService referenceService,
            IMessageService messageService,
            ILogger<ReferenceController> logger)
            : base(messageService, logger)
        {
            _referenceService = referenceService;
        }

        /// <summary>
        /// Get all supported payment methods
        /// </summary>
        [HttpGet("payment-methods")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get payment methods", Description = "Returns all supported payment method codes (e.g. cash, bank).")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetPaymentMethods()
        {
            return Ok(_referenceService.GetPaymentMethods(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("business-type-statuses")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get business type statuses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetBusinessTypeStatuses()
        {
            return Ok(_referenceService.GetBusinessTypeStatuses(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("cost-types")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get cost types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetCostTypes()
        {
            return Ok(_referenceService.GetCostTypes(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("general-ledger-reference-types")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get general ledger reference types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetGeneralLedgerReferenceTypes()
        {
            return Ok(_referenceService.GetGeneralLedgerReferenceTypes(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("general-ledger-transaction-types")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get general ledger transaction types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetGeneralLedgerTransactionTypes()
        {
            return Ok(_referenceService.GetGeneralLedgerTransactionTypes(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("general-ledger-view-modes")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get general ledger view modes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetGeneralLedgerViewModes()
        {
            return Ok(_referenceService.GetGeneralLedgerViewModes(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("import-statuses")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get import statuses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetImportStatuses()
        {
            return Ok(_referenceService.GetImportStatuses(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("import-types")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get import types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetImportTypes()
        {
            return Ok(_referenceService.GetImportTypes(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("money-channel-types")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get money channel types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetMoneyChannelTypes()
        {
            return Ok(_referenceService.GetMoneyChannelTypes(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("order-statuses")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get order statuses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetOrderStatuses()
        {
            return Ok(_referenceService.GetOrderStatuses(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("product-statuses")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get product statuses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetProductStatuses()
        {
            return Ok(_referenceService.GetProductStatuses(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("revenue-types")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get revenue types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetRevenueTypes()
        {
            return Ok(_referenceService.GetRevenueTypes(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("stock-movement-types")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get stock movement types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetStockMovementTypes()
        {
            return Ok(_referenceService.GetStockMovementTypes(), MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("stock-movement-reference-types")]
        [OutputCache(PolicyName = "PublicData")]
        [SwaggerOperation(Summary = "Get stock movement reference types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetStockMovementReferenceTypes()
        {
            return Ok(_referenceService.GetStockMovementReferenceTypes(), MessageKeys.DataRetrievedSuccessfully);
        }
    }
}
