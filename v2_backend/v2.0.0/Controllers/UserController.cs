using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
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

        private string CallerUserUid => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "";
        private bool CallerIsAdmin   => User.IsInRole("admin");

        // ── endpoints ─────────────────────────────────────────────────────────────

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<CreateUserResponseDto>> CreateUserAsync(CreateUserRequestDto createUserRequestDto)
        {
            try
            {
                var createdUserResponse = await _userService.CreateUserAsync(createUserRequestDto);
                return CreatedAtAction(nameof(GetUserProfileDataAsync), new { userId = createdUserResponse.UserId }, createdUserResponse);
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

        // Self-delete: no userId param — account is identified from the JWT sub claim
        [HttpDelete]
        public async Task<ActionResult> DeleteMyAccountAsync()
        {
            try
            {
                await _userService.DeleteMyAccountAsync(CallerUserUid);
                return NoContent();
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
    }
}
