using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class TokenBlacklistRepository : ITokenBlacklistRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<TokenBlacklistRepository> _logger;

        public TokenBlacklistRepository(VaxtrackDbContext dbContext, ILogger<TokenBlacklistRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task RevokeTokenAsync(string jti, DateTime expiresAt)
        {
            ArgumentNullException.ThrowIfNull(jti);

            try
            {
                _dbContext.RevokedTokens.Add(new RevokedTokenModel
                {
                    Jti       = jti,
                    ExpiresAt = expiresAt,
                    RevokedAt = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();

                // Opportunistic cleanup: purge rows whose tokens have naturally expired already
                var expired = _dbContext.RevokedTokens.Where(t => t.ExpiresAt <= DateTime.UtcNow);
                _dbContext.RevokedTokens.RemoveRange(expired);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TokenBlacklistRepository: RevokeTokenAsync - {Message}", ex.Message);
                throw new Exception($"TokenBlacklistRepository: RevokeTokenAsync - {ex.Message}", ex);
            }
        }

        public async Task<bool> IsRevokedAsync(string jti)
        {
            ArgumentNullException.ThrowIfNull(jti);

            try
            {
                return await _dbContext.RevokedTokens
                    .AnyAsync(t => t.Jti == jti && t.ExpiresAt > DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TokenBlacklistRepository: IsRevokedAsync - {Message}", ex.Message);
                throw new Exception($"TokenBlacklistRepository: IsRevokedAsync - {ex.Message}", ex);
            }
        }
    }
}
