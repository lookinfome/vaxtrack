using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vaxtrack.Interfaces;
using Vaxtrack.Dtos.UserDtos;

namespace Vaxtrack.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/vaxtrack/v1/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // ── helpers ───────────────────────────────────────────────────────────────

        private string CallerUserUid =>
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        private bool CallerIsAdmin   => User.IsInRole("admin");

        // ── endpoints ─────────────────────────────────────────────────────────────

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<CreateUserResponseDto>> CreateUserAsync(CreateUserRequestDto createUserRequestDto)
        {
            try
            {
                var createdUserResponse = await _userService.CreateUserAsync(createUserRequestDto);
                return CreatedAtAction("GetUserProfileData", new { userId = createdUserResponse.UserId }, createdUserResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: CreateUserAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut]
        public async Task<ActionResult<UpdateUserResponseDto>> UpdateUserAsync(UpdateUserRequestDto updateUserRequestDto)
        {
            try
            {
                var updatedUserResponse = await _userService.UpdateUserAsync(updateUserRequestDto, CallerUserUid, CallerIsAdmin);
                return Ok(updatedUserResponse);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: UpdateUserAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut]
        public async Task<ActionResult<UpdateEmailResponseDto>> UpdateEmailAsync(UpdateEmailRequestDto updateEmailRequestDto)
        {
            try
            {
                var response = await _userService.UpdateEmailAsync(updateEmailRequestDto, CallerUserUid, CallerIsAdmin);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: UpdateEmailAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut]
        public async Task<ActionResult<ChangePasswordResponseDto>> ChangePasswordAsync(ChangePasswordRequestDto changePasswordRequestDto)
        {
            try
            {
                var response = await _userService.ChangePasswordAsync(changePasswordRequestDto, CallerUserUid, CallerIsAdmin);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: ChangePasswordAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<UpdateUserResponseDto>> UploadProfilePictureAsync([FromForm] string userId, [FromForm] IFormFile file)
        {
            try
            {
                var response = await _userService.UploadProfilePictureAsync(userId, file, CallerUserUid, CallerIsAdmin);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: UploadProfilePictureAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut("{userId}")]
        public async Task<ActionResult<UpdateUserResponseDto>> RemoveProfilePictureAsync(string userId)
        {
            try
            {
                var response = await _userService.RemoveProfilePictureAsync(userId, CallerUserUid, CallerIsAdmin);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: RemoveProfilePictureAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult<List<UserProfileDataDto>>> GetAllUsersAsync()
        {
            try
            {
                List<UserProfileDataDto> allUsers = await _userService.GetAllUsersAsync();
                return Ok(allUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: GetAllUsersAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Backs the Users Management tab — filterable, paginated
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult<PagedUsersResponseDto>> GetUsersPagedAsync(
            [FromQuery] string? name, [FromQuery] string? phone, [FromQuery] string? userId, [FromQuery] string? userUid,
            [FromQuery] string? role, [FromQuery] string? vaccinationStatus, [FromQuery] string? sortBy, [FromQuery] string? sortDir,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _userService.GetUsersPagedAsync(name, phone, userId, userUid, role, vaccinationStatus, sortBy, sortDir, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: GetUsersPagedAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<UserProfileDataDto>> GetUserProfileDataAsync(string userId)
        {
            try
            {
                var userProfileData = await _userService.GetUserProfileDataAsync(userId, CallerUserUid, CallerIsAdmin);
                return Ok(userProfileData);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: GetUserProfileDataAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Self-delete: no userId param — account is identified from the JWT sub claim.
        // Requires password re-entry in the body (see Support page's Delete My Account flow).
        [HttpDelete]
        public async Task<ActionResult> DeleteMyAccountAsync([FromBody] DeleteMyAccountRequestDto body)
        {
            try
            {
                await _userService.DeleteMyAccountAsync(CallerUserUid, body.Password, body.Reason);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: DeleteMyAccountAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{userId}")]
        public async Task<ActionResult> DeleteUserAsync(string userId)
        {
            try
            {
                await _userService.DeleteUserAsync(userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: DeleteUserAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // ── lifecycle ─────────────────────────────────────────────────────────────

        [Authorize(Roles = "admin")]
        [HttpPut("{userId}")]
        public async Task<ActionResult<UserProfileDataDto>> DisableUserAsync(string userId, UserActionCommentRequestDto body)
        {
            try
            {
                var updated = await _userService.DisableUserAsync(userId, CallerUserUid, CallerIsAdmin, body?.Comment ?? "");
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: DisableUserAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{userId}")]
        public async Task<ActionResult<UserProfileDataDto>> ApproveUserReactivationAsync(string userId, UserActionCommentRequestDto? body)
        {
            try
            {
                var updated = await _userService.ApproveUserReactivationAsync(userId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: ApproveUserReactivationAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{userId}")]
        public async Task<ActionResult<UserProfileDataDto>> RejectUserReactivationAsync(string userId, UserActionCommentRequestDto? body)
        {
            try
            {
                var updated = await _userService.RejectUserReactivationAsync(userId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: RejectUserReactivationAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{userId}")]
        public async Task<ActionResult<UserProfileDataDto>> PromoteToAdminAsync(string userId)
        {
            try
            {
                var updated = await _userService.PromoteToAdminAsync(userId, CallerUserUid, CallerIsAdmin);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: PromoteToAdminAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{userId}")]
        public async Task<ActionResult<UserProfileDataDto>> DemoteFromAdminAsync(string userId)
        {
            try
            {
                var updated = await _userService.DemoteFromAdminAsync(userId, CallerUserUid, CallerIsAdmin);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: DemoteFromAdminAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
