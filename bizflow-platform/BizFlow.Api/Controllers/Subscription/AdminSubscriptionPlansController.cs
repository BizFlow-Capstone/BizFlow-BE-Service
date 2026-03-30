using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Subscription;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers.Subscription
{
    [Route("api/admin/subscription-plans")]
    [Authorize]
    public class AdminSubscriptionPlansController : BaseApiController
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public AdminSubscriptionPlansController(
            ISubscriptionPlanService subscriptionPlanService,
            IMessageService messageService,
            ILogger<AdminSubscriptionPlansController> logger)
            : base(messageService, logger)
        {
            _subscriptionPlanService = subscriptionPlanService;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] SubscriptionPlanQueryParams query)
        {
            User.EnsureAdminRole();
            var result = await _subscriptionPlanService.SearchPlansAsync(query);
            return OkPaginated(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            User.EnsureAdminRole();
            var plan = await _subscriptionPlanService.GetPlanByIdAsync(id);
            if (plan == null)
                return NotFound(MessageKeys.SubscriptionPlanNotFound);

            return Ok(plan, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanRequest request)
        {
            User.EnsureAdminRole();
            var plan = await _subscriptionPlanService.CreatePlanAsync(request);
            return Ok(plan, MessageKeys.DataCreatedSuccessfully);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateSubscriptionPlanRequest request)
        {
            User.EnsureAdminRole();
            var plan = await _subscriptionPlanService.UpdatePlanAsync(id, request);
            return Ok(plan, MessageKeys.DataUpdatedSuccessfully);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            User.EnsureAdminRole();
            await _subscriptionPlanService.DeletePlanAsync(id);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetPlanStatusRequest request)
        {
            User.EnsureAdminRole();
            var plan = await _subscriptionPlanService.SetPlanStatusAsync(id, request.IsActive);
            return Ok(plan, MessageKeys.DataUpdatedSuccessfully);
        }

    }
}
