using Vaxtrack.Models;

namespace Vaxtrack.Interfaces.RepositoryInterfaces
{
    public interface IUserRoleMappingRepository
    {
        Task<UserRoleMappingModel> AddRoleMappingAsync(UserRoleMappingModel mapping);
        Task<UserRoleMappingModel> UpdateRoleMappingAsync(UserRoleMappingModel mapping);
        Task<UserRoleMappingModel?> GetRoleMappingByIdAsync(int id);
        Task<List<UserRoleMappingModel>> GetRoleMappingsByUserUidAsync(string userUid);
        Task<List<UserRoleMappingModel>> GetRoleMappingsByRoleTagAsync(string roleTag, string contextId);
        Task<UserRoleMappingModel?> GetExistingMappingAsync(string userUid, string roleTag, string contextId);
        Task<bool> IsUserInRoleAsync(string userUid, string roleTag, string contextId);
        Task RevokeAllMappingsByUserUidAsync(string userUid);
        Task RevokeAllMappingsByContextIdAsync(string contextId);
    }
}
