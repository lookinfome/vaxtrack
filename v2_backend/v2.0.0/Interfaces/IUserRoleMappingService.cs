using Vaxtrack.Dtos.UserRoleMappingDtos;

namespace Vaxtrack.Interfaces
{
    public interface IUserRoleMappingService
    {
        Task<AssignRoleResponseDto> AssignRoleAsync(AssignRoleRequestDto request);
        Task RevokeRoleAsync(int mappingId);
        Task<List<UserRoleMappingProfileDto>> GetUserRolesAsync(string userUid);
        Task<List<UserRoleMappingProfileDto>> GetUsersInRoleAsync(string roleTag, string contextId);
        Task<bool> IsUserInRoleAsync(string userUid, string roleTag, string contextId);
        Task RevokeUserRoleMappingsAsync(string userUid);
    }
}
