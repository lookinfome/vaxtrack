using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class UserCredentialsRepository : IUserCredentialsRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<UserCredentialsRepository> _logger;

        public UserCredentialsRepository(VaxtrackDbContext dbContext, ILogger<UserCredentialsRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserCredentialsModel> CreateCredentialsAsync(UserCredentialsModel credentials)
        {
            ArgumentNullException.ThrowIfNull(credentials);

            try
            {
                _dbContext.UserCredentials.Add(credentials);
                await _dbContext.SaveChangesAsync();
                return credentials;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCredentialsRepository: CreateCredentialsAsync - {Message}", ex.Message);
                throw new Exception($"UserCredentialsRepository: CreateCredentialsAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserCredentialsModel?> GetCredentialsByEmailAsync(string email)
        {
            ArgumentNullException.ThrowIfNull(email);

            try
            {
                return await _dbContext.UserCredentials
                    .Where(c => c.Email == email && !c.IsDeleted)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCredentialsRepository: GetCredentialsByEmailAsync - {Message}", ex.Message);
                throw new Exception($"UserCredentialsRepository: GetCredentialsByEmailAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserCredentialsModel?> GetCredentialsByUserUidAsync(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                return await _dbContext.UserCredentials
                    .Where(c => c.UserUid == userUid && !c.IsDeleted)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCredentialsRepository: GetCredentialsByUserUidAsync - {Message}", ex.Message);
                throw new Exception($"UserCredentialsRepository: GetCredentialsByUserUidAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserCredentialsModel>> GetCredentialsByUserUidsAsync(List<string> userUids)
        {
            ArgumentNullException.ThrowIfNull(userUids);

            try
            {
                return await _dbContext.UserCredentials
                    .Where(c => userUids.Contains(c.UserUid) && !c.IsDeleted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCredentialsRepository: GetCredentialsByUserUidsAsync - {Message}", ex.Message);
                throw new Exception($"UserCredentialsRepository: GetCredentialsByUserUidsAsync - {ex.Message}", ex);
            }
        }

        public async Task SoftDeleteCredentialsByUserUidAsync(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                var credentials = await _dbContext.UserCredentials
                    .Where(c => c.UserUid == userUid && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (credentials is null)
                    return;

                credentials.IsDeleted    = true;
                credentials.DeletedAt    = DateTime.UtcNow;
                credentials.PasswordHash = "";   // clear hash — no purpose after deletion, reduces exposure if DB is compromised
                credentials.UpdatedAt    = DateTime.UtcNow;

                _dbContext.UserCredentials.Update(credentials);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCredentialsRepository: SoftDeleteCredentialsByUserUidAsync - {Message}", ex.Message);
                throw new Exception($"UserCredentialsRepository: SoftDeleteCredentialsByUserUidAsync - {ex.Message}", ex);
            }
        }

        public async Task UpdateEmailAsync(string userUid, string newEmail)
        {
            ArgumentNullException.ThrowIfNull(userUid);
            ArgumentNullException.ThrowIfNull(newEmail);

            try
            {
                var credentials = await _dbContext.UserCredentials
                    .Where(c => c.UserUid == userUid && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (credentials is null)
                    throw new Exception($"UserCredentialsRepository: UpdateEmailAsync - no active credentials found for UserUid {userUid}");

                credentials.Email     = newEmail;
                credentials.UpdatedAt = DateTime.UtcNow;

                _dbContext.UserCredentials.Update(credentials);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCredentialsRepository: UpdateEmailAsync - {Message}", ex.Message);
                throw new Exception($"UserCredentialsRepository: UpdateEmailAsync - {ex.Message}", ex);
            }
        }

        public async Task UpdatePasswordHashAsync(string userUid, string newPasswordHash)
        {
            ArgumentNullException.ThrowIfNull(userUid);
            ArgumentNullException.ThrowIfNull(newPasswordHash);

            try
            {
                var credentials = await _dbContext.UserCredentials
                    .Where(c => c.UserUid == userUid && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (credentials is null)
                    throw new Exception($"UserCredentialsRepository: UpdatePasswordHashAsync - no active credentials found for UserUid {userUid}");

                credentials.PasswordHash = newPasswordHash;
                credentials.UpdatedAt    = DateTime.UtcNow;

                _dbContext.UserCredentials.Update(credentials);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCredentialsRepository: UpdatePasswordHashAsync - {Message}", ex.Message);
                throw new Exception($"UserCredentialsRepository: UpdatePasswordHashAsync - {ex.Message}", ex);
            }
        }
    }
}
