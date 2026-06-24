using Vaxtrack.Dtos.UserDtos;

namespace Vaxtrack.Interfaces
{
    public interface IUserService
    {
        Task<CreateUserResponseDto> CreateUserAsync(CreateUserRequestDto createUserRequestDto);

        // callerUserUid + callerIsAdmin: enforce that only the account owner (or an admin) can update/view
        Task<UpdateUserResponseDto> UpdateUserAsync(UpdateUserRequestDto updateUserRequestDto, string callerUserUid, bool callerIsAdmin);
        Task<UserProfileDataDto> GetUserProfileDataAsync(string userId, string callerUserUid, bool callerIsAdmin);
        Task<List<UserProfileDataDto>> GetAllUsersAsync();

        Task<UpdateEmailResponseDto> UpdateEmailAsync(UpdateEmailRequestDto updateEmailRequestDto, string callerUserUid, bool callerIsAdmin);
        Task<ChangePasswordResponseDto> ChangePasswordAsync(ChangePasswordRequestDto changePasswordRequestDto, string callerUserUid, bool callerIsAdmin);

        // Admin-initiated delete (by UserId); self-delete (by callerUserUid from JWT)
        Task DeleteUserAsync(string userId);
        Task DeleteMyAccountAsync(string callerUserUid);
    }
}
