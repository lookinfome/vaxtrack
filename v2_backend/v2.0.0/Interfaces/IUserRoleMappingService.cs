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

        // ── hospital-admin application (Support page) ─────────────────────────────
        Task<UserRequestDto> SubmitHospitalAdminApplicationAsync(string callerUserUid, string hospitalId, string? comment);
        Task<UserRequestDto> ApproveHospitalAdminApplicationAsync(int requestId, string callerUserUid, bool callerIsAdmin, string? comment);
        Task<UserRequestDto> RejectHospitalAdminApplicationAsync(int requestId, string callerUserUid, bool callerIsAdmin, string? comment);

        // Admin-only — feeds the Support page's approval queue (both request types)
        Task<List<UserRequestDto>> GetPendingRequestsAsync();
    }
}
