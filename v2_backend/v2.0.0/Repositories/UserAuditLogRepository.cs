using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class UserAuditLogRepository : IUserAuditLogRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<UserAuditLogRepository> _logger;

        public UserAuditLogRepository(VaxtrackDbContext dbContext, ILogger<UserAuditLogRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserAuditLogModel> CreateEntryAsync(UserAuditLogModel entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            try
            {
                _dbContext.UserAuditLogs.Add(entry);
                await _dbContext.SaveChangesAsync();
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserAuditLogRepository: CreateEntryAsync - {Message}", ex.Message);
                throw new Exception($"UserAuditLogRepository: CreateEntryAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserAuditLogModel>> GetEntriesByUserIdAsync(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                return await _dbContext.UserAuditLogs
                    .Where(e => e.UserId == userId)
                    .OrderBy(e => e.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserAuditLogRepository: GetEntriesByUserIdAsync - {Message}", ex.Message);
                throw new Exception($"UserAuditLogRepository: GetEntriesByUserIdAsync - {ex.Message}", ex);
            }
        }
    }
}
