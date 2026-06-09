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

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<ActionResult<CreateUserResponseDto>> CreateUserAsync(CreateUserRequestDto createUserRequestDto)
        {
            try
            {
                var createdUserResponse = await _userService.CreateUserAsync(createUserRequestDto);
                return Ok(createdUserResponse);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
            catch (ArgumentException ex)
            {                
                return BadRequest(ex.Message);
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
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}