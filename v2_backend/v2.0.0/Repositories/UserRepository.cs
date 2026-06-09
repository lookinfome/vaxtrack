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

        public async Task<UserModel> AddUserAsync(UserModel userDetails)
        {
            _dbContext.Users.Add(userDetails);
            await _dbContext.SaveChangesAsync();
            return userDetails;
        }

        public async Task<UserModel> UpdateUserAsync(UserModel userDetails)
        {
            var foundUser = await GetUserByIdAsync(userDetails.UserId);

            // Update the user properties
            foundUser?.UserName = userDetails.UserName;
            foundUser?.UserGender = userDetails.UserGender;
            foundUser?.UserPhone = userDetails.UserPhone;
            foundUser?.UpdatedAt = userDetails.UpdatedAt;
            foundUser?.ProfilePicturePath = userDetails.ProfilePicturePath;

            _dbContext.Users.Update(foundUser!);
            await _dbContext.SaveChangesAsync();
            return foundUser!;
        }

        public async Task<UserModel?> GetUserByIdAsync(string userId)
        {
            var foundUser = await _dbContext.Users.FindAsync(userId);
            if (foundUser == null)
            {
                throw new ArgumentException("User not found");
            }

            return foundUser;
        }

        public async Task<List<UserModel>?> GetAllUsersAsync()
        {
            return await _dbContext.Users.Where(u => !u.IsDeleted).ToListAsync();    
        }

        public async Task DeleteUserAsync(string userId, DateTime deletedAt, bool isDeleted)
        {
            var foundUser = await GetUserByIdAsync(userId);
            foundUser?.IsDeleted = isDeleted;
            foundUser?.DeletedAt = deletedAt;
            foundUser?.UpdatedAt = deletedAt;
            _dbContext.Users.Update(foundUser!);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            return await _dbContext.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted);
        }

    }
}
