using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Debtor;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;

namespace BizFlow.Api.Controllers.Debtor
{
    /// <summary>
    /// Debtor and debt management APIs.
    /// </summary>
    [Route("api/my-business/debtors")]
    public class DebtorController : PaginatedApiController
    {
        private readonly IDebtorService _debtorService;


        public DebtorController(
            IDebtorService debtorService,
            IMessageService messageService,
            IOptions<PaginationSettings> paginationSettings,
            ILogger<DebtorController> logger)
            : base(messageService, logger, paginationSettings)
        {
            _debtorService = debtorService;
        }

        /// <summary>
        /// Returns debtors with pagination and filters.
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "List/Filter debtors", Description = "Returns paginated list of debtors for a business location.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> List([FromQuery] DebtorQueryParams query)
        {
            var userId = GetCurrentUserId();
            var result = await _debtorService.ListAsync(userId, query);
            return OkPaginated(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Returns debtor details by debtor ID.
        /// </summary>
        [HttpGet("{debtorId:long}")]
        [SwaggerOperation(Summary = "Get debtor details")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDetail(long debtorId)
        {
            var userId = GetCurrentUserId();
            var result = await _debtorService.GetDetailAsync(userId, debtorId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Returns all active debtors for a specific location.
        /// </summary>
        [HttpGet("locations/{locationId:int}")]
        [SwaggerOperation(Summary = "Get active debtors by location (minimal)")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActiveByLocation(int locationId)
        {
            var userId = GetCurrentUserId();
            var result = await _debtorService.GetActiveDebtorsByLocationAsync(userId, locationId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Creates a new debtor.
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Create debtor")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateDebtorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(MessageKeys.ValidationError, ModelState);

            var userId = GetCurrentUserId();
            var result = await _debtorService.CreateAsync(userId, request);
            return Created(result, MessageKeys.DataCreatedSuccessfully, nameof(GetDetail), new { debtorId = result.DebtorId });
        }

        /// <summary>
        /// Updates debtor information.
        /// </summary>
        [HttpPut("{debtorId:long}")]
        [SwaggerOperation(Summary = "Update debtor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(long debtorId, [FromBody] UpdateDebtorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(MessageKeys.ValidationError, ModelState);

            var userId = GetCurrentUserId();
            var result = await _debtorService.UpdateAsync(userId, debtorId, request);
            return Ok(result, MessageKeys.DataUpdatedSuccessfully);
        }

        /// <summary>
        /// Toggles debtor active status.
        /// </summary>
        [HttpPatch("{debtorId:long}/status")]
        [SwaggerOperation(Summary = "Update debtor status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(long debtorId, [FromBody] UpdateDebtorStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(MessageKeys.ValidationError, ModelState);

            var userId = GetCurrentUserId();
            var result = await _debtorService.UpdateStatusAsync(userId, debtorId, request.IsActive);
            return Ok(result, MessageKeys.DataUpdatedSuccessfully);
        }

        /// <summary>
        /// Soft-deletes a debtor. Rejects when outstanding balance exists.
        /// </summary>
        [HttpDelete("{debtorId:long}")]
        [SwaggerOperation(Summary = "Delete debtor (soft)")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(long debtorId, [FromQuery] bool force = false)
        {
            var userId = GetCurrentUserId();
            await _debtorService.DeleteAsync(userId, debtorId, force);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        /// <summary>
        /// Records a debt payment/adjustment using absolute amount and explicit action.
        /// </summary>
        [HttpPost("{debtorId:long}/payments")]
        [SwaggerOperation(
            Summary = "Record debt adjustment",
            Description = "Amount must be positive. Action determines direction: decrease_debt (reduce receivable) or increase_debt (increase receivable).")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RecordPayment(long debtorId, [FromBody] RecordDebtPaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(MessageKeys.ValidationError, ModelState);

            var userId = GetCurrentUserId();
            var result = await _debtorService.RecordPaymentAsync(userId, debtorId, request);
            return Created(result, MessageKeys.DataCreatedSuccessfully, nameof(GetPayments), new { debtorId });
        }

        /// <summary>
        /// Returns debt payment history for a debtor.
        /// </summary>
        [HttpGet("{debtorId:long}/payments")]
        [SwaggerOperation(Summary = "Get debt payment history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPayments(long debtorId)
        {
            var userId = GetCurrentUserId();
            var result = await _debtorService.GetPaymentsAsync(userId, debtorId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        #region Private Helper Methods

        private Guid GetCurrentUserId() => User.GetRequiredUserId();

        #endregion
    }
}
