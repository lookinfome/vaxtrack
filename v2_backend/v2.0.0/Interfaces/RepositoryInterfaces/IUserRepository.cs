using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IUserRepository
    {
        Task<UserModel> AddUserAsync(UserModel userDetails);
        Task<UserModel> UpdateUserAsync(UserModel userDetails);
        Task<UserModel?> GetUserByIdAsync(string userId);
        Task<List<UserModel>?> GetAllUsersAsync();
        Task DeleteUserAsync(string userId, DateTime deletedAt, bool isDeleted);
        Task<bool> UserExistsAsync(string userId);
    }
}
