using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using BizFlow.Api.Common.Extensions;

namespace BizFlow.Api.Controllers
{
    /// <summary>
    /// Manages employee hiring operations
    /// </summary>
    [Authorize]
    [Route("api/my-employee")]
    public class HireController : BaseApiController
    {
        private readonly IHireService _hireService;

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
        /// Gets basic employee list with id and name only (for selection/dropdowns)
        /// </summary>
        [HttpGet("employees")]
        [SwaggerOperation(Summary = "Get employee list", Description = "Returns hired employees (id + name only). For dropdowns/selection.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyHiredEmployees()
        {
            var response = await _hireService.GetEmployeeSummariesAsync(GetCurrentUserId());
            return Ok(response, MessageKeys.DataRetrievedSuccessfully);
        }

        /// <summary>
        /// Gets detailed employee list with full information (for management)
        /// </summary>
        [HttpGet("employees/details")]
        [SwaggerOperation(Summary = "Get employee details", Description = "Returns complete employee information. For management/admin pages.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyHiredEmployeeDetails()
        {
            var employees = await _hireService.GetHiredEmployeeDetailsAsync(GetCurrentUserId());
            return Ok(employees, MessageKeys.DataRetrievedSuccessfully);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets current user ID securely from JWT token claims
        /// </summary>
        private Guid GetCurrentUserId()
        {
            return User.GetRequiredUserId();
        }

        #endregion
    }
}
