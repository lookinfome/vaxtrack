using Microsoft.AspNetCore.Http;
using Vaxtrack.Dtos.UserDtos;

namespace Vaxtrack.Interfaces
{
    public interface IUserService
    {
        Task<CreateUserResponseDto> CreateUserAsync(CreateUserRequestDto createUserRequestDto);

        // callerUserUid + callerIsAdmin: enforce that only the account owner (or an admin) can update/view
        Task<UpdateUserResponseDto> UpdateUserAsync(UpdateUserRequestDto updateUserRequestDto, string callerUserUid, bool callerIsAdmin);
        Task<UserProfileDataDto> GetUserProfileDataAsync(string userId, string callerUserUid, bool callerIsAdmin);
        Task<List<UserProfileDataDto>> GetAllUsersAsync();

        // Admin-only paginated/filtered listing that backs the Users Management tab.
        // Filters are all optional and case-insensitive substring matches; done in-memory
        // (dataset is small) to avoid complex EF query-building for a rarely-changing table.
        Task<PagedUsersResponseDto> GetUsersPagedAsync(string? name, string? phone, string? userId, string? userUid, int page, int pageSize);

        Task<UpdateEmailResponseDto> UpdateEmailAsync(UpdateEmailRequestDto updateEmailRequestDto, string callerUserUid, bool callerIsAdmin);
        Task<ChangePasswordResponseDto> ChangePasswordAsync(ChangePasswordRequestDto changePasswordRequestDto, string callerUserUid, bool callerIsAdmin);

        // Validates, resizes, and re-encodes the uploaded image before storing it under a fixed
        // per-user filename (auto-overwrites any previous picture — no unbounded storage growth).
        Task<UpdateUserResponseDto> UploadProfilePictureAsync(string userId, IFormFile file, string callerUserUid, bool callerIsAdmin);

        // Admin-initiated delete (by UserId)
        Task DeleteUserAsync(string userId);

        // Self-delete: requires re-entering the current password as a safety confirmation
        // (same re-auth pattern as HospitalService.AuthorizeUnregisterAsync).
        Task DeleteMyAccountAsync(string callerUserUid, string password, string? reason);

        // ── lifecycle ────────────────────────────────────────────────────────────
        // Active -> Disabled: platform admin only, reason required. Immediately invalidates
        // any live session for this user (checked in Program.cs's OnTokenValidated) and blocks
        // future logins (checked in AuthService.LoginAsync).
        Task<UserProfileDataDto> DisableUserAsync(string userId, string callerUserUid, bool callerIsAdmin, string comment);

        // PendingReactivation -> Active / Disabled: platform admin only.
        Task<UserProfileDataDto> ApproveUserReactivationAsync(string userId, string callerUserUid, bool callerIsAdmin, string? comment);
        Task<UserProfileDataDto> RejectUserReactivationAsync(string userId, string callerUserUid, bool callerIsAdmin, string? comment);

        // Disabled -> PendingReactivation: called from the public (anonymous) reactivation
        // request flow in AuthService, identified by UserUid rather than a caller principal.
        Task MarkPendingReactivationAsync(string userUid);

        // Platform-admin-only role toggles — no endpoint existed for this before now.
        Task<UserProfileDataDto> PromoteToAdminAsync(string userId, string callerUserUid, bool callerIsAdmin);
        Task<UserProfileDataDto> DemoteFromAdminAsync(string userId, string callerUserUid, bool callerIsAdmin);
    }
}
