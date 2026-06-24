using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vaxtrack.Dtos.UserRoleMappingDtos;
using Vaxtrack.Interfaces;

namespace Vaxtrack.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("/api/vaxtrack/v1/[controller]/[action]")]
    public class UserRoleMappingController : ControllerBase
    {
        private readonly IUserRoleMappingService _roleMappingService;
        private readonly ILogger<UserRoleMappingController> _logger;

        public UserRoleMappingController(IUserRoleMappingService roleMappingService, ILogger<UserRoleMappingController> logger)
        {
            _roleMappingService = roleMappingService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<AssignRoleResponseDto>> AssignRoleAsync(AssignRoleRequestDto assignRoleRequestDto)
        {
            try
            {
                var assignedRole = await _roleMappingService.AssignRoleAsync(assignRoleRequestDto);
                return CreatedAtAction(nameof(GetUserRolesAsync), new { userUid = assignedRole.UserUid }, assignedRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: AssignRoleAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{mappingId}")]
        public async Task<ActionResult> RevokeRoleAsync(int mappingId)
        {
            try
            {
                await _roleMappingService.RevokeRoleAsync(mappingId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: RevokeRoleAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{userUid}")]
        public async Task<ActionResult<List<UserRoleMappingProfileDto>>> GetUserRolesAsync(string userUid)
        {
            try
            {
                var userRoles = await _roleMappingService.GetUserRolesAsync(userUid);
                return Ok(userRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: GetUserRolesAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // contextId is optional — omit for all contexts, supply to narrow to a specific entity
        [HttpGet("{roleTag}")]
        public async Task<ActionResult<List<UserRoleMappingProfileDto>>> GetUsersInRoleAsync(string roleTag, [FromQuery] string contextId = "")
        {
            try
            {
                var usersInRole = await _roleMappingService.GetUsersInRoleAsync(roleTag, contextId);
                return Ok(usersInRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: GetUsersInRoleAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // contextId is optional — omit to check role regardless of scope
        [HttpGet("{userUid}/{roleTag}")]
        public async Task<ActionResult<bool>> IsUserInRoleAsync(string userUid, string roleTag, [FromQuery] string contextId = "")
        {
            try
            {
                var result = await _roleMappingService.IsUserInRoleAsync(userUid, roleTag, contextId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: IsUserInRoleAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
