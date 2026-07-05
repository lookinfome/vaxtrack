using Microsoft.EntityFrameworkCore;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Repositories
{
    public class UserRequestRepository : IUserRequestRepository
    {
        private readonly VaxtrackDbContext _dbContext;
        private readonly ILogger<UserRequestRepository> _logger;

        public UserRequestRepository(VaxtrackDbContext dbContext, ILogger<UserRequestRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserRequestModel> CreateAsync(UserRequestModel request)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                _dbContext.UserRequests.Add(request);
                await _dbContext.SaveChangesAsync();
                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRequestRepository: CreateAsync - {Message}", ex.Message);
                throw new Exception($"UserRequestRepository: CreateAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserRequestModel> UpdateAsync(UserRequestModel request)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                _dbContext.UserRequests.Update(request);
                await _dbContext.SaveChangesAsync();
                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRequestRepository: UpdateAsync - {Message}", ex.Message);
                throw new Exception($"UserRequestRepository: UpdateAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserRequestModel?> GetByIdAsync(int id)
        {
            try
            {
                return await _dbContext.UserRequests.FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRequestRepository: GetByIdAsync - {Message}", ex.Message);
                throw new Exception($"UserRequestRepository: GetByIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserRequestModel>> GetPendingByTypeAsync(string requestType)
        {
            ArgumentNullException.ThrowIfNull(requestType);

            try
            {
                return await _dbContext.UserRequests
                    .Where(r => r.RequestType == requestType && r.Status == "Pending")
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRequestRepository: GetPendingByTypeAsync - {Message}", ex.Message);
                throw new Exception($"UserRequestRepository: GetPendingByTypeAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserRequestModel>> GetAllPendingAsync()
        {
            try
            {
                return await _dbContext.UserRequests
                    .Where(r => r.Status == "Pending")
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRequestRepository: GetAllPendingAsync - {Message}", ex.Message);
                throw new Exception($"UserRequestRepository: GetAllPendingAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserRequestModel>> GetByUserUidAsync(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                return await _dbContext.UserRequests
                    .Where(r => r.UserUid == userUid)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRequestRepository: GetByUserUidAsync - {Message}", ex.Message);
                throw new Exception($"UserRequestRepository: GetByUserUidAsync - {ex.Message}", ex);
            }
        }
    }
}
