using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class UserRoleMappingRepository : IUserRoleMappingRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<UserRoleMappingRepository> _logger;

        public UserRoleMappingRepository(VaxtrackDbContext dbContext, ILogger<UserRoleMappingRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserRoleMappingModel> AddRoleMappingAsync(UserRoleMappingModel mapping)
        {
            ArgumentNullException.ThrowIfNull(mapping);

            try
            {
                _dbContext.UserRoleMappings.Add(mapping);
                await _dbContext.SaveChangesAsync();
                return mapping;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingRepository: AddRoleMappingAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingRepository: AddRoleMappingAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserRoleMappingModel> UpdateRoleMappingAsync(UserRoleMappingModel mapping)
        {
            ArgumentNullException.ThrowIfNull(mapping);

            try
            {
                _dbContext.UserRoleMappings.Update(mapping);
                await _dbContext.SaveChangesAsync();
                return mapping;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingRepository: UpdateRoleMappingAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingRepository: UpdateRoleMappingAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserRoleMappingModel?> GetRoleMappingByIdAsync(int id)
        {
            try
            {
                return await _dbContext.UserRoleMappings
                    .Where(r => r.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingRepository: GetRoleMappingByIdAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingRepository: GetRoleMappingByIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserRoleMappingModel>> GetRoleMappingsByUserUidAsync(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                return await _dbContext.UserRoleMappings
                    .Where(r => r.UserUid == userUid && r.IsActive)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingRepository: GetRoleMappingsByUserUidAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingRepository: GetRoleMappingsByUserUidAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserRoleMappingModel>> GetRoleMappingsByRoleTagAsync(string roleTag, string contextId)
        {
            ArgumentNullException.ThrowIfNull(roleTag);

            try
            {
                // Empty contextId returns all users with this role regardless of scope;
                // non-empty contextId narrows to a specific entity (e.g. a specific hospital)
                var query = _dbContext.UserRoleMappings
                    .Where(r => r.RoleTag == roleTag && r.IsActive);

                if (!string.IsNullOrEmpty(contextId))
                    query = query.Where(r => r.ContextId == contextId);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingRepository: GetRoleMappingsByRoleTagAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingRepository: GetRoleMappingsByRoleTagAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserRoleMappingModel?> GetExistingMappingAsync(string userUid, string roleTag, string contextId)
        {
            ArgumentNullException.ThrowIfNull(userUid);
            ArgumentNullException.ThrowIfNull(roleTag);

            try
            {
                return await _dbContext.UserRoleMappings
                    .Where(r => r.UserUid == userUid && r.RoleTag == roleTag && r.ContextId == contextId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingRepository: GetExistingMappingAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingRepository: GetExistingMappingAsync - {ex.Message}", ex);
            }
        }

        public async Task RevokeAllMappingsByUserUidAsync(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                var activeMappings = await _dbContext.UserRoleMappings
                    .Where(r => r.UserUid == userUid && r.IsActive)
                    .ToListAsync();

                var timestamp = DateTime.UtcNow;
                foreach (var mapping in activeMappings)
                {
                    mapping.IsActive = false;
                    mapping.UpdatedAt = timestamp;
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingRepository: RevokeAllMappingsByUserUidAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingRepository: RevokeAllMappingsByUserUidAsync - {ex.Message}", ex);
            }
        }

        public async Task RevokeAllMappingsByContextIdAsync(string contextId)
        {
            ArgumentNullException.ThrowIfNull(contextId);

            try
            {
                // Revoke all active role mappings scoped to this context (e.g. all hospital-admin
                // assignments for a hospital that is being soft-deleted)
                var activeMappings = await _dbContext.UserRoleMappings
                    .Where(r => r.ContextId == contextId && r.IsActive)
                    .ToListAsync();

                var timestamp = DateTime.UtcNow;
                foreach (var mapping in activeMappings)
                {
                    mapping.IsActive = false;
                    mapping.UpdatedAt = timestamp;
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingRepository: RevokeAllMappingsByContextIdAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingRepository: RevokeAllMappingsByContextIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<bool> IsUserInRoleAsync(string userUid, string roleTag, string contextId)
        {
            ArgumentNullException.ThrowIfNull(userUid);
            ArgumentNullException.ThrowIfNull(roleTag);

            try
            {
                // Empty contextId checks for the role regardless of scope;
                // non-empty contextId checks for the role within that specific scope
                var query = _dbContext.UserRoleMappings
                    .Where(r => r.UserUid == userUid && r.RoleTag == roleTag && r.IsActive);

                if (!string.IsNullOrEmpty(contextId))
                    query = query.Where(r => r.ContextId == contextId);

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingRepository: IsUserInRoleAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingRepository: IsUserInRoleAsync - {ex.Message}", ex);
            }
        }
    }
}
