using Vaxtrack.Dtos.UserDtos;

namespace Vaxtrack.Interfaces
{
    public interface IUserService
    {
        Task<CreateUserResponseDto> CreateUserAsync(CreateUserRequestDto createUserRequestDto);
        Task<UpdateUserResponseDto> UpdateUserAsync(UpdateUserRequestDto updateUserRequestDto);
        Task<UserProfileDataDto> GetUserProfileDataAsync(string userId);
        Task<List<UserProfileDataDto>> GetAllUsersAsync();
        Task DeleteUserAsync(string userId);
    }
}
