using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers
{
    [Route("api/test")]
    [Authorize]
    public class TestController : BaseApiController
    {
        private readonly IFirestoreService _firestoreService;

        public TestController(
            IFirestoreService firestoreService,
            IMessageService messageService,
            ILogger<TestController> logger)
            : base(messageService, logger)
        {
            _firestoreService = firestoreService;
        }

        [HttpGet("firebase/firestore/health")]
        public async Task<IActionResult> GetFirestoreHealth()
        {
            User.EnsureAdminRole();
            var status = await _firestoreService.GetHealthStatusAsync();
            return Ok(status, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("firebase/firestore/usage")]
        public async Task<IActionResult> GetFirestoreUsage()
        {
            User.EnsureAdminRole();
            var ownerProfileId = User.GetRequiredUserId();
            var docId = $"{ownerProfileId}_active";
            var health = await _firestoreService.GetHealthStatusAsync();
            var usageDoc = await _firestoreService.GetUsageDocAsync(docId);

            return Ok(
                new
                {
                    health,
                    docId,
                    exists = usageDoc != null,
                    usage = usageDoc
                },
                MessageKeys.DataRetrievedSuccessfully
            );
        }

        [HttpGet("firebase/firestore/gate-debug")]
        public async Task<IActionResult> GetFirestoreGateDebug()
        {
            User.EnsureAdminRole();
            _ = User.GetRequiredUserId();
            var gate = await _firestoreService.GetFirestoreGateDebugAsync();
            return Ok(gate, MessageKeys.DataRetrievedSuccessfully);
        }
    }
}
