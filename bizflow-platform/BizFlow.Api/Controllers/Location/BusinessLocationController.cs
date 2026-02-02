using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Location
{
    /// <summary>
    /// Business Location Management APIs
    /// </summary>
    [Route("api/location")]
    public class BusinessLocationController : BaseApiController
    {
        private readonly IBusinessLocationService _locationService;

        // TODO: Replace with actual user service when authentication is implemented
        // Mock user ID for testing (Shinkiri from mock data)
        private static readonly Guid _mockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
        private static readonly Guid _employeeId = Guid.Parse("550e8400-e29b-41d4-a716-446655440003");

        public BusinessLocationController(
            IBusinessLocationService locationService,
            IMessageService messageService,
            ILogger<BusinessLocationController> logger)
            : base(messageService, logger)
        {
            _locationService = locationService;
        }

        #region Owner APIs

        /// <summary>
        /// Gets all locations owned by current user
        /// </summary>
        [HttpGet("me/owned")]
        [SwaggerOperation(Summary = "Get all business locations owned by current user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOwnedLocations()
        {
            var userId = GetCurrentUserId();
            var locations = await _locationService.GetOwnedLocationsAsync(userId);
            return Ok(locations, MessageKeys.LocationsRetrievedSuccessfully);
        }

        /// <summary>
        /// Creates a new business location (current user becomes owner)
        /// </summary>
        [HttpPost("create")]
        [SwaggerOperation(Summary = "Create a new business location")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
        {
            var userId = GetCurrentUserId();
            var location = await _locationService.CreateLocationAsync(userId, request);
            return Created(location, MessageKeys.LocationCreatedSuccessfully, nameof(GetOwnedLocations), null!);
        }

        /// <summary>
        /// Updates location active status (owner only)
        /// </summary>
        [HttpPut("me/owned/{id:int}/status")]
        [SwaggerOperation(Summary = "Enable or disable a location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLocationStatus(int id, [FromBody] UpdateLocationStatusRequest request)
        {
            var userId = GetCurrentUserId();
            var success = await _locationService.UpdateLocationStatusAsync(userId, id, request.IsActive);
            
            if (!success)
            {
                return Forbidden(MessageKeys.LocationAccessDenied);
            }

            return Ok(MessageKeys.LocationStatusUpdated);
        }

        /// <summary>
        /// Updates location information (owner only)
        /// </summary>
        [HttpPut("me/owned/{id:int}")]
        [SwaggerOperation(Summary = "Update location details (name, address, phone)")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationRequest request)
        {
            var userId = GetCurrentUserId();
            var success = await _locationService.UpdateLocationAsync(userId, id, request);
            
            if (!success)
            {
                return Forbidden(MessageKeys.LocationAccessDenied);
            }

            return Ok(MessageKeys.LocationUpdatedSuccessfully);
        }

        /// <summary>
        /// Adds employees to a location (owner only)
        /// </summary>
        [HttpPost("{locationId:int}/employees")]
        [SwaggerOperation(Summary = "Assign hired employees to a location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddEmployeesToLocation(int locationId, [FromBody] List<Guid> employeeIds)
        {
            var userId = GetCurrentUserId();
            var success = await _locationService.AddEmployeesToLocationAsync(userId, locationId, employeeIds);
            
            if (!success)
            {
                return Forbidden(MessageKeys.LocationAccessDenied);
            }

            return Ok(MessageKeys.EmployeesAddedSuccessfully);
        }

        #endregion

        #region Employee APIs

        /// <summary>
        /// Gets all locations where current user works at (as employee)
        /// </summary>
        [HttpGet("work-at-locations")]
        [SwaggerOperation(Summary = "Get locations where current user is assigned to work")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWorkAtLocations()
        {
            //var userId = GetCurrentUserId();
            var userId = _employeeId;
            var locations = await _locationService.GetWorkLocationsAsync(userId);
            return Ok(locations, MessageKeys.LocationsRetrievedSuccessfully);
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
