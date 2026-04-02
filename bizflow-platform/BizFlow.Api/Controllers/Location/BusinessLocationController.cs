using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Api.Common.Filters;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Location
{
    /// <summary>
    /// Business Location Management APIs
    /// </summary>
    [Authorize]
    [Route("api/location")]
    public class BusinessLocationController : BaseApiController
    {
        private readonly IBusinessLocationService _locationService;

        public BusinessLocationController(
            IBusinessLocationService locationService,
            IMessageService messageService,
            ILogger<BusinessLocationController> logger)
            : base(messageService, logger)
        {
            _locationService = locationService;
        }

        #region Owner APIs

        [HttpGet("me/owned")]
        [SwaggerOperation(Summary = "Get owned locations", Description = "Returns all locations where user is owner.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOwnedLocations()
        {
            var userId = GetCurrentUserId();
            var locations = await _locationService.GetOwnedLocationsAsync(userId);
            return Ok(locations, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("create")]
        [RequireFeature(FeatureCodes.Locations)]
        [SwaggerOperation(Summary = "Create location", Description = "Creates new location and assigns current user as owner.")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
        {
            var userId = GetCurrentUserId();
            var location = await _locationService.CreateLocationAsync(userId, request);
            return Created(location, MessageKeys.DataCreatedSuccessfully, nameof(GetOwnedLocations), null!);
        }

        [HttpPatch("me/owned/{locationId:int}/status")]
        [SwaggerOperation(Summary = "Update location status", Description = "Toggle IsActive flag. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLocationStatus(int locationId, [FromBody] UpdateLocationStatusRequest request)
        {
            var userId = GetCurrentUserId();
            await _locationService.UpdateLocationStatusAsync(userId, locationId, request.IsActive);
            return Ok(MessageKeys.DataUpdatedSuccessfully);
        }

        [HttpPut("me/owned/{locationId:int}")]
        [SwaggerOperation(Summary = "Update location info", Description = "Update name, address, phone, city. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLocation(int locationId, [FromBody] UpdateLocationRequest request)
        {
            var userId = GetCurrentUserId();
            await _locationService.UpdateLocationAsync(userId, locationId, request);
            return Ok(MessageKeys.DataUpdatedSuccessfully);
        }

        [HttpPost("{locationId:int}/employees")]
        [SwaggerOperation(Summary = "Assign employees", Description = "Add employees to location. Must be hired first. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddEmployeesToLocation(int locationId, [FromBody] List<Guid> employeeIds)
        {
            var userId = GetCurrentUserId();
            await _locationService.AddEmployeesToLocationAsync(userId, locationId, employeeIds);
            return Ok(MessageKeys.DataCreatedSuccessfully);
        }

        [HttpDelete("{locationId:int}/employees/{employeeId:guid}")]
        [SwaggerOperation(Summary = "Remove employee", Description = "Remove an employee from a location. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveEmployeeFromLocation(int locationId, Guid employeeId)
        {
            var userId = GetCurrentUserId();
            await _locationService.RemoveEmployeeFromLocationAsync(userId, locationId, employeeId);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        [HttpDelete("me/owned/{locationId:int}")]
        [SwaggerOperation(Summary = "Delete location", Description = "Soft delete - sets DeletedAt. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteLocation(int locationId)
        {
            var userId = GetCurrentUserId();
            await _locationService.DeleteLocationAsync(userId, locationId);
            return Ok(MessageKeys.DataDeletedSuccessfully);
        }

        [HttpGet("me/owned/{locationId:int}/employees")]
        [SwaggerOperation(Summary = "Get location employees", Description = "Returns employees assigned to this location. Owner only.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployeesByLocation(int locationId)
        {
            var userId = GetCurrentUserId();
            var result = await _locationService.GetEmployeesByLocationAsync(userId, locationId);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        #endregion

        #region Shared APIs (Owner + Employee)

        [HttpGet("{locationId:int}")]
        [SwaggerOperation(Summary = "Get location detail", Description = "Returns location detail. Owner or assigned Employee.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLocationDetail(int locationId)
        {
            var userId = GetCurrentUserId();
            var detail = await _locationService.GetLocationDetailAsync(userId, locationId);
            return Ok(detail, MessageKeys.DataRetrievedSuccessfully);
        }

        #endregion

        #region Employee APIs

        [HttpGet("work-at-locations")]
        [SwaggerOperation(Summary = "Get work locations", Description = "Returns locations where current user works as employee.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWorkAtLocations()
        {
            var userId = GetCurrentUserId();
            var locations = await _locationService.GetWorkLocationsAsync(userId);
            return Ok(locations, MessageKeys.DataRetrievedSuccessfully);
        }

        #endregion

        #region Private Helpers

        private Guid GetCurrentUserId() => User.GetRequiredUserId();

        #endregion
    }
}
