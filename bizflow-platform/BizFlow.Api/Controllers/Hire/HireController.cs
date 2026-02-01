using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers
{
    /// <summary>
    /// Controller for managing employee hiring
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

        /// <summary>
        /// Get current user ID (mock for now)
        /// </summary>
        private Guid GetCurrentUserId()
        {
            // TODO: Get from JWT claims when auth is implemented
            // return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            return _mockCurrentUserId;
        }

        /// <summary>
        /// Get all employees hired by the current user (owner)
        /// </summary>
        /// <returns>List of hired employees</returns>
        [HttpGet("me/employees")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyHiredEmployees()
        {
            try
            {
                var employees = await _hireService.GetHiredEmployeesAsync(GetCurrentUserId());

                return Ok(employees, MessageKeys.HireEmployeesRetrievedSuccessfully);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
