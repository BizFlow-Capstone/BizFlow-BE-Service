using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers
{
    /// <summary>
    /// Manages employee hiring operations
    /// </summary>
    [Route("api/hire")]
    public class HireController : BaseApiController
    {
        private readonly IHireService _hireService;

        // TODO: Replace with actual JWT-based user identification
        private readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"); // Shinkiri

        public HireController(
            IHireService hireService,
            IMessageService messageService,
            ILogger<HireController> logger)
            : base(messageService, logger)
        {
            _hireService = hireService;
        }

        #region API Endpoints

        /// <summary>
        /// Gets all employees hired by current user
        /// </summary>
        [HttpGet("me/employees")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyHiredEmployees()
        {
            var employees = await _hireService.GetHiredEmployeesAsync(GetCurrentUserId());
            return Ok(employees, MessageKeys.HireEmployeesRetrievedSuccessfully);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets current user ID (mock implementation)
        /// TODO: Replace with JWT claims when auth is implemented
        /// </summary>
        private Guid GetCurrentUserId()
        {
            // return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            return _mockCurrentUserId;
        }

        #endregion
    }
}
