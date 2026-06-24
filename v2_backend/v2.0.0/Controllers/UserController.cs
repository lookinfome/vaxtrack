using Microsoft.AspNetCore.Mvc;
using Vaxtrack.Interfaces;
using Vaxtrack.Dtos.UserDtos;

namespace Vaxtrack.Controllers
{
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
                var updatedUserResponse = await _userService.UpdateUserAsync(updateUserRequestDto);
                return Ok(updatedUserResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: UpdateUserAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

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
                var userProfileData = await _userService.GetUserProfileDataAsync(userId);
                return Ok(userProfileData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController: GetUserProfileDataAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

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
