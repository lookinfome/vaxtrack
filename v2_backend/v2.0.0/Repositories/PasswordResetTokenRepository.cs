using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<PasswordResetTokenRepository> _logger;

        public PasswordResetTokenRepository(VaxtrackDbContext dbContext, ILogger<PasswordResetTokenRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<PasswordResetTokenModel> CreateTokenAsync(string userUid, string token, DateTime expiresAt)
        {
            ArgumentNullException.ThrowIfNull(userUid);
            ArgumentNullException.ThrowIfNull(token);

            try
            {
                var resetToken = new PasswordResetTokenModel
                {
                    UserUid   = userUid,
                    Token     = token,
                    IsUsed    = false,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow,
                    UsedAt    = null
                };

                _dbContext.PasswordResetTokens.Add(resetToken);
                await _dbContext.SaveChangesAsync();
                return resetToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PasswordResetTokenRepository: CreateTokenAsync - {Message}", ex.Message);
                throw new Exception($"PasswordResetTokenRepository: CreateTokenAsync - {ex.Message}", ex);
            }
        }

        public async Task<PasswordResetTokenModel?> GetValidTokenAsync(string token)
        {
            ArgumentNullException.ThrowIfNull(token);

            try
            {
                return await _dbContext.PasswordResetTokens
                    .Where(t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PasswordResetTokenRepository: GetValidTokenAsync - {Message}", ex.Message);
                throw new Exception($"PasswordResetTokenRepository: GetValidTokenAsync - {ex.Message}", ex);
            }
        }

        public async Task MarkTokenAsUsedAsync(int tokenId)
        {
            try
            {
                var resetToken = await _dbContext.PasswordResetTokens.FindAsync(tokenId);

                if (resetToken is null)
                    throw new Exception($"PasswordResetTokenRepository: MarkTokenAsUsedAsync - token {tokenId} not found");

                resetToken.IsUsed = true;
                resetToken.UsedAt = DateTime.UtcNow;

                _dbContext.PasswordResetTokens.Update(resetToken);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PasswordResetTokenRepository: MarkTokenAsUsedAsync - {Message}", ex.Message);
                throw new Exception($"PasswordResetTokenRepository: MarkTokenAsUsedAsync - {ex.Message}", ex);
            }
        }
    }
}
