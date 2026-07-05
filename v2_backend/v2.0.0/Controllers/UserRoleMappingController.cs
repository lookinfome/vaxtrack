using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vaxtrack.Dtos.UserRoleMappingDtos;
using Vaxtrack.Interfaces;

namespace Vaxtrack.Controllers
{
    [Authorize]
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

        // ── helpers ───────────────────────────────────────────────────────────────

        private string CallerUserUid =>
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        private bool CallerIsAdmin   => User.IsInRole("admin");

        // ── endpoints ─────────────────────────────────────────────────────────────

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<AssignRoleResponseDto>> AssignRoleAsync(AssignRoleRequestDto assignRoleRequestDto)
        {
            try
            {
                var assignedRole = await _roleMappingService.AssignRoleAsync(assignRoleRequestDto);
                return CreatedAtAction("GetUserRoles", new { userUid = assignedRole.UserUid }, assignedRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: AssignRoleAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
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

        // Self-or-admin — lets a logged-in user (e.g. a hospital-admin) discover their own
        // scoped roles, since that assignment is never encoded in the JWT itself.
        [HttpGet("{userUid}")]
        public async Task<ActionResult<List<UserRoleMappingProfileDto>>> GetUserRolesAsync(string userUid)
        {
            try
            {
                if (userUid != CallerUserUid && !CallerIsAdmin)
                    return Forbid();

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
        [Authorize(Roles = "admin")]
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

        // contextId is optional — omit to check role regardless of scope; self-or-admin
        [HttpGet("{userUid}/{roleTag}")]
        public async Task<ActionResult<bool>> IsUserInRoleAsync(string userUid, string roleTag, [FromQuery] string contextId = "")
        {
            try
            {
                if (userUid != CallerUserUid && !CallerIsAdmin)
                    return Forbid();

                var result = await _roleMappingService.IsUserInRoleAsync(userUid, roleTag, contextId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: IsUserInRoleAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // ── hospital-admin application (Support page) ─────────────────────────────

        [HttpPost]
        public async Task<ActionResult<UserRequestDto>> SubmitHospitalAdminApplicationAsync(SubmitHospitalAdminApplicationRequestDto requestDto)
        {
            try
            {
                var request = await _roleMappingService.SubmitHospitalAdminApplicationAsync(CallerUserUid, requestDto.HospitalId, requestDto.Comment);
                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: SubmitHospitalAdminApplicationAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{requestId}")]
        public async Task<ActionResult<UserRequestDto>> ApproveHospitalAdminApplicationAsync(int requestId, RequestActionCommentRequestDto? body)
        {
            try
            {
                var request = await _roleMappingService.ApproveHospitalAdminApplicationAsync(requestId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(request);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: ApproveHospitalAdminApplicationAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{requestId}")]
        public async Task<ActionResult<UserRequestDto>> RejectHospitalAdminApplicationAsync(int requestId, RequestActionCommentRequestDto? body)
        {
            try
            {
                var request = await _roleMappingService.RejectHospitalAdminApplicationAsync(requestId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(request);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: RejectHospitalAdminApplicationAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult<List<UserRequestDto>>> GetPendingRequestsAsync()
        {
            try
            {
                var pending = await _roleMappingService.GetPendingRequestsAsync();
                return Ok(pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingController: GetPendingRequestsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
