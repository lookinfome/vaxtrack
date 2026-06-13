using Vaxtrack.Models;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;

namespace Vaxtrack.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly VaxtrackDbContext _dbContext;

        public UserRepository(VaxtrackDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserModel> CreateUserAsync(UserModel userCreateRequest)
        {
            ArgumentNullException.ThrowIfNull(userCreateRequest);

            _dbContext.Users.Add(userCreateRequest);
            await _dbContext.SaveChangesAsync();
            return userCreateRequest;
        }

        public async Task<UserModel> UpdateUserAsync(UserModel userUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(userUpdateRequest);

            _dbContext.Users.Update(userUpdateRequest);
            await _dbContext.SaveChangesAsync();
            return userUpdateRequest;
        }

        public async Task<UserModel?> GetUserDetailsByUserIdAsync(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            var foundUser = await _dbContext.Users.Where(u=>u.UserId == userId && !u.IsDeleted).FirstOrDefaultAsync();
            return foundUser;
        }

        public async Task<List<UserModel>?> GetAllUsersDetailAsync()
        {
            return await _dbContext.Users.Where(u => !u.IsDeleted).ToListAsync();   
        }

        public async Task DeleteUserAsync(UserModel userDeleteRequest)
        {
            ArgumentNullException.ThrowIfNull(userDeleteRequest);

            _dbContext.Users.Update(userDeleteRequest);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsUserExists(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            return await _dbContext.Users.AnyAsync(u=>u.UserId == userId && !u.IsDeleted);
        }


    }
}
