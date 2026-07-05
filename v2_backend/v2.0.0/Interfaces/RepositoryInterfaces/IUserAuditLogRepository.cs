using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IUserAuditLogRepository
    {
        Task<UserAuditLogModel> CreateEntryAsync(UserAuditLogModel entry);
        Task<List<UserAuditLogModel>> GetEntriesByUserIdAsync(string userId);
    }
}
