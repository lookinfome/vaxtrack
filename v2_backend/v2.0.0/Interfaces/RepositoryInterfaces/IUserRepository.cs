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
        Task DeleteUserAsync(UserModel userDeleteRequest);
        Task<bool> IsUserExists(string userId);
    }
}
