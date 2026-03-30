using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers.Subscription
{
    [Route("api/admin/features")]
    [Authorize]
    public class AdminFeaturesController : BaseApiController
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public AdminFeaturesController(
            ISubscriptionPlanService subscriptionPlanService,
            IMessageService messageService,
            ILogger<AdminFeaturesController> logger)
            : base(messageService, logger)
        {
            _subscriptionPlanService = subscriptionPlanService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            User.EnsureAdminRole();
            var features = await _subscriptionPlanService.GetAllFeaturesAsync();
            return Ok(features, MessageKeys.DataRetrievedSuccessfully);
        }
    }
}
