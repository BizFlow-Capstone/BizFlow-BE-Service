using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

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
        private static readonly Guid MockCurrentUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

        public BusinessLocationController(
            IBusinessLocationService locationService,
            IMessageService messageService,
            ILogger<BusinessLocationController> logger)
            : base(messageService, logger)
        {
            _locationService = locationService;
        }

        /// <summary>
        /// Get current user ID (mock for now)
        /// </summary>
        private Guid GetCurrentUserId()
        {
            // TODO: Get from JWT claims when auth is implemented
            // return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            return MockCurrentUserId;
        }

        #region Owner APIs

        /// <summary>
        /// Get all locations owned by current user
        /// </summary>
        [HttpGet("me/owned")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOwnedLocations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var locations = await _locationService.GetOwnedLocationsAsync(userId);
                return Ok(locations, MessageKeys.LocationsRetrievedSuccessfully);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Create a new business location (current user becomes owner)
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var location = await _locationService.CreateLocationAsync(userId, request);
                return Created(location, MessageKeys.LocationCreatedSuccessfully, nameof(GetOwnedLocations), null!);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Update location active status (owner only)
        /// </summary>
        [HttpPut("me/owned/{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLocationStatus(int id, [FromBody] UpdateLocationStatusRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _locationService.UpdateLocationStatusAsync(userId, id, request.IsActive);
                
                if (!success)
                {
                    return Forbidden(MessageKeys.LocationAccessDenied);
                }

                return Ok(MessageKeys.LocationStatusUpdated);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Update location information (owner only)
        /// </summary>
        [HttpPut("me/owned/{id:int}/update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _locationService.UpdateLocationAsync(userId, id, request);
                
                if (!success)
                {
                    return Forbidden(MessageKeys.LocationAccessDenied);
                }

                return Ok(MessageKeys.LocationUpdatedSuccessfully);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region Employee APIs

        /// <summary>
        /// Get all locations where current user works at (as employee)
        /// </summary>
        [HttpGet("work-at-locations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWorkAtLocations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var locations = await _locationService.GetWorkLocationsAsync(userId);
                return Ok(locations, MessageKeys.LocationsRetrievedSuccessfully);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion
    }
}
