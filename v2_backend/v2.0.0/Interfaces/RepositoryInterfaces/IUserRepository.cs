using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IUserRepository
    {
        Task<UserModel> CreateUserAsync(UserModel userCreateRequest);
        Task<UserModel> UpdateUserAsync(UserModel userUpdateRequest);
        Task<UserModel?> GetUserDetailsByUserIdAsync(string userId);
        Task<UserModel?> GetUserDetailsByUserUidAsync(string userUid);
        Task<List<UserModel>?> GetAllUsersDetailAsync();

        // Lightweight bulk lookup (UserUid/UserId/UserName only) for screens that need to
        // label a list of UserUids with a readable name — e.g. hospital-admin lists, pending requests.
        Task<List<UserModel>> GetUsersByUserUidsAsync(List<string> userUids);
        Task DeleteUserAsync(UserModel userDeleteRequest);
        Task<bool> IsUserExists(string userId);
    }
}
