using Vaxtrack.Models;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;

namespace Vaxtrack.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(VaxtrackDbContext dbContext, ILogger<UserRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserModel> CreateUserAsync(UserModel userCreateRequest)
        {
            ArgumentNullException.ThrowIfNull(userCreateRequest);

            try
            {
                _dbContext.Users.Add(userCreateRequest);
                await _dbContext.SaveChangesAsync();
                return userCreateRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRepository: CreateUserAsync - {Message}", ex.Message);
                throw new Exception($"UserRepository: CreateUserAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserModel> UpdateUserAsync(UserModel userUpdateRequest)
        {
            ArgumentNullException.ThrowIfNull(userUpdateRequest);

            try
            {
                _dbContext.Users.Update(userUpdateRequest);
                await _dbContext.SaveChangesAsync();
                return userUpdateRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRepository: UpdateUserAsync - {Message}", ex.Message);
                throw new Exception($"UserRepository: UpdateUserAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserModel?> GetUserDetailsByUserIdAsync(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                return await _dbContext.Users.Where(u => u.UserId == userId && !u.IsDeleted).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRepository: GetUserDetailsByUserIdAsync - {Message}", ex.Message);
                throw new Exception($"UserRepository: GetUserDetailsByUserIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserModel?> GetUserDetailsByUserUidAsync(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                return await _dbContext.Users.Where(u => u.UserUid == userUid && !u.IsDeleted).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRepository: GetUserDetailsByUserUidAsync - {Message}", ex.Message);
                throw new Exception($"UserRepository: GetUserDetailsByUserUidAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserModel>?> GetAllUsersDetailAsync()
        {
            try
            {
                return await _dbContext.Users.Where(u => !u.IsDeleted).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRepository: GetAllUsersDetailAsync - {Message}", ex.Message);
                throw new Exception($"UserRepository: GetAllUsersDetailAsync - {ex.Message}", ex);
            }
        }

        public async Task DeleteUserAsync(UserModel userDeleteRequest)
        {
            ArgumentNullException.ThrowIfNull(userDeleteRequest);

            try
            {
                _dbContext.Users.Update(userDeleteRequest);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRepository: DeleteUserAsync - {Message}", ex.Message);
                throw new Exception($"UserRepository: DeleteUserAsync - {ex.Message}", ex);
            }
        }

        public async Task<bool> IsUserExists(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                return await _dbContext.Users.AnyAsync(u => u.UserId == userId && !u.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRepository: IsUserExists - {Message}", ex.Message);
                throw new Exception($"UserRepository: IsUserExists - {ex.Message}", ex);
            }
        }
    }
}
