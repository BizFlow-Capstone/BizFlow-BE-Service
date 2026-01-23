using BizFlow.Api.Common.Controllers;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Controllers
{
    [Route("api/roles")]
    public class RoleController : BaseApiController
    {
        private readonly IRoleService _roleService;

        public RoleController(
            IRoleService roleService,
            IMessageService messageService,
            ILogger<RoleController> logger)
            : base(messageService, logger)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Get all roles in table role in database
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                return Ok(roles, MessageKeys.RolesRetrievedSuccessfully);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Get role by Id
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    return NotFound(MessageKeys.RoleNotFound, id);
                }

                return Ok(role, MessageKeys.RolesRetrievedSuccessfully);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Get role by name
        /// </summary>
        [HttpGet("by-name/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleByName(string name)
        {
            try
            {
                var role = await _roleService.GetRoleByNameAsync(name);
                if (role == null)
                {
                    return NotFound(MessageKeys.RoleNotFound);
                }

                return Ok(role, MessageKeys.RolesRetrievedSuccessfully);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
