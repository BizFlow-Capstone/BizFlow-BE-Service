using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers.Subscription
{
    [Route("api/subscription-plans")]
    public class SubscriptionPlansController : BaseApiController
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public SubscriptionPlansController(
            ISubscriptionPlanService subscriptionPlanService,
            IMessageService messageService,
            ILogger<SubscriptionPlansController> logger)
            : base(messageService, logger)
        {
            _subscriptionPlanService = subscriptionPlanService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _subscriptionPlanService.GetActivePlansAsync();
            return Ok(plans, MessageKeys.DataRetrievedSuccessfully);
        }
    }
}
