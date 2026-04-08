using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Order;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Order
{
    [Route("api/my-business/accounting")]
    public class OrderController : PaginatedApiController
    {
        private readonly IOrderService _orderService;

        public OrderController(
            IOrderService orderService,
            IMessageService messageService,
            IOptions<PaginationSettings> paginationSettings,
            ILogger<OrderController> logger)
            : base(messageService, logger, paginationSettings)
        {
            _orderService = orderService;
        }

        [HttpPost("orders")]
        [SwaggerOperation(Summary = "Create order pending")]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(MessageKeys.ValidationError, ModelState);

            var result = await _orderService.CreateAsync(GetCurrentUserId(), request);
            if (result.RequiresConfirmation)
                return Ok(result, MessageKeys.DataRetrievedSuccessfully, result.Warnings);

            return Created(result, MessageKeys.DataCreatedSuccessfully, nameof(GetDetail), new { orderId = result.Order?.OrderId }, result.Warnings);
        }

        [HttpPut("orders/{orderId:long}")]
        [SwaggerOperation(Summary = "Update order by status", Description = "Updates a pending order directly, or for a completed order creates one replacement (using idempotencyKey) and cancels the old order.")]
        public async Task<IActionResult> Update(long orderId, [FromBody] UpdateOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(MessageKeys.ValidationError, ModelState);

            var result = await _orderService.UpdateAsync(GetCurrentUserId(), orderId, request);
            if (result.RequiresConfirmation)
                return Ok(result, MessageKeys.DataRetrievedSuccessfully, result.Warnings);

            return Ok(result, MessageKeys.DataUpdatedSuccessfully, result.Warnings);
        }

        [HttpPost("orders/{orderId:long}/complete")]
        [SwaggerOperation(Summary = "Complete pending order")]
        public async Task<IActionResult> Complete(long orderId, [FromBody] CompleteOrderRequest request)
        {
            var result = await _orderService.CompleteAsync(GetCurrentUserId(), orderId, request);
            if (result.RequiresConfirmation)
                return Ok(result, MessageKeys.DataRetrievedSuccessfully, result.Warnings);

            return Ok(result, MessageKeys.DataUpdatedSuccessfully, result.Warnings);
        }

        [HttpPost("orders/{orderId:long}/cancel")]
        [SwaggerOperation(Summary = "Cancel order")]
        public async Task<IActionResult> Cancel(long orderId, [FromBody] CancelOrderRequest request)
        {
            var result = await _orderService.CancelAsync(GetCurrentUserId(), orderId, request);
            return Ok(result, MessageKeys.DataUpdatedSuccessfully);
        }

        [HttpGet("orders")]
        [SwaggerOperation(Summary = "List orders")]
        public async Task<IActionResult> List([FromQuery] OrderQueryParams query)
        {
            ApplyPaginationDefaults(query);
            var result = await _orderService.ListAsync(GetCurrentUserId(), query);
            return OkPaginated(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("orders/{orderId:long}")]
        [SwaggerOperation(Summary = "Get order detail")]
        public async Task<IActionResult> GetDetail(long orderId)
        {
            var result = await _orderService.GetDetailAsync(GetCurrentUserId(), orderId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        private Guid GetCurrentUserId() => User.GetRequiredUserId();
    }
}
