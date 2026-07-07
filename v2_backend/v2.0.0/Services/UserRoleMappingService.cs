using Vaxtrack.Dtos.UserRoleMappingDtos;
using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;

namespace Vaxtrack.Services
{
    public class UserRoleMappingService : IUserRoleMappingService
    {
        private readonly IUserRoleMappingRepository _roleMappingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHospitalRepository _hospitalRepository;
        private readonly IUserRequestRepository _userRequestRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<UserRoleMappingService> _logger;

        public UserRoleMappingService(
            IUserRoleMappingRepository roleMappingRepository,
            IUserRepository userRepository,
            IHospitalRepository hospitalRepository,
            IUserRequestRepository userRequestRepository,
            INotificationService notificationService,
            ILogger<UserRoleMappingService> logger)
        {
            _roleMappingRepository = roleMappingRepository;
            _userRepository = userRepository;
            _hospitalRepository = hospitalRepository;
            _userRequestRepository = userRequestRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<AssignRoleResponseDto> AssignRoleAsync(AssignRoleRequestDto request)
        {
            /*
             * Assign Role Logic:
             * ------------------
             * Assigns a role tag (optionally scoped to a context) to a user.
             * A user+role+context combination is unique — the same role cannot be
             * assigned twice for the same scope.
             *
             * If a previously revoked mapping (IsActive = false) exists for the same
             * user+role+context, it is reactivated rather than creating a duplicate row.
             * This keeps the audit trail intact.
             *
             * Edge cases blocked:
             *   - Null request        → ArgumentNullException thrown before entering try.
             *   - User not found      → throws.
             *   - Role already active → throws (prevent duplicate active assignments).
             */

            ArgumentNullException.ThrowIfNull(request);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserUidAsync(request.UserUid);
                if (foundUser is null)
                    throw new Exception($"UserRoleMappingService: AssignRoleAsync - user {request.UserUid} not found");

                var existingMapping = await _roleMappingRepository.GetExistingMappingAsync(
                    request.UserUid, request.RoleTag, request.ContextId);

                if (existingMapping is not null)
                {
                    if (existingMapping.IsActive)
                        throw new Exception($"UserRoleMappingService: AssignRoleAsync - user {request.UserUid} already has active role '{request.RoleTag}' for context '{request.ContextId}'");

                    // Reactivate a previously revoked mapping
                    existingMapping.IsActive = true;
                    existingMapping.UpdatedAt = DateTime.UtcNow;
                    var reactivated = await _roleMappingRepository.UpdateRoleMappingAsync(existingMapping);
                    await _notificationService.NotifyAsync(request.UserUid, $"You have been assigned the role '{request.RoleTag}'.");
                    return MapToAssignRoleResponseDto(reactivated);
                }

                var timestamp = DateTime.UtcNow;
                var newMapping = new UserRoleMappingModel
                {
                    UserUid = request.UserUid,
                    RoleTag = request.RoleTag,
                    ContextId = request.ContextId,
                    IsActive = true,
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp
                };

                var createdMapping = await _roleMappingRepository.AddRoleMappingAsync(newMapping);
                await _notificationService.NotifyAsync(request.UserUid, $"You have been assigned the role '{request.RoleTag}'.");
                return MapToAssignRoleResponseDto(createdMapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: AssignRoleAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: AssignRoleAsync - {ex.Message}", ex);
            }
        }

        public async Task RevokeRoleAsync(int mappingId)
        {
            /*
             * Revoke Role Logic:
             * ------------------
             * Soft-revokes a role assignment by setting IsActive = false.
             * The record is kept for audit trail — revoked roles can be reactivated
             * via AssignRoleAsync without creating a duplicate row.
             *
             * Edge cases blocked:
             *   - Mapping not found       → throws.
             *   - Role already revoked    → throws (idempotency guard).
             */

            try
            {
                var foundMapping = await _roleMappingRepository.GetRoleMappingByIdAsync(mappingId);

                if (foundMapping is null)
                    throw new Exception($"UserRoleMappingService: RevokeRoleAsync - role mapping {mappingId} not found");

                if (!foundMapping.IsActive)
                    throw new Exception($"UserRoleMappingService: RevokeRoleAsync - role mapping {mappingId} is already revoked");

                foundMapping.IsActive = false;
                foundMapping.UpdatedAt = DateTime.UtcNow;
                await _roleMappingRepository.UpdateRoleMappingAsync(foundMapping);
                await _notificationService.NotifyAsync(foundMapping.UserUid, $"Your role '{foundMapping.RoleTag}' has been removed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: RevokeRoleAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: RevokeRoleAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserRoleMappingProfileDto>> GetUserRolesAsync(string userUid)
        {
            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                var foundMappings = await _roleMappingRepository.GetRoleMappingsByUserUidAsync(userUid);

                List<UserRoleMappingProfileDto> roleList = [];
                foreach (var mapping in foundMappings)
                    roleList.Add(MapToProfileDto(mapping));

                return roleList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: GetUserRolesAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: GetUserRolesAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserRoleMappingProfileDto>> GetUsersInRoleAsync(string roleTag, string contextId)
        {
            /*
             * Get Users In Role Logic:
             * ------------------------
             * Returns all active role assignments for a given role tag.
             * Pass an empty contextId to get all users with this role across any scope.
             * Pass a specific contextId (e.g. a HospitalId) to narrow to that scope only —
             * useful for finding the approvers for a specific entity in a ServiceNow-like flow.
             */

            ArgumentNullException.ThrowIfNull(roleTag);

            try
            {
                var foundMappings = await _roleMappingRepository.GetRoleMappingsByRoleTagAsync(roleTag, contextId);

                var userUids = foundMappings.Select(m => m.UserUid).Distinct().ToList();
                var users = await _userRepository.GetUsersByUserUidsAsync(userUids);
                var userByUid = users.ToDictionary(u => u.UserUid);

                List<UserRoleMappingProfileDto> roleList = [];
                foreach (var mapping in foundMappings)
                {
                    userByUid.TryGetValue(mapping.UserUid, out var user);
                    roleList.Add(MapToProfileDto(mapping, user?.UserId, user?.UserName));
                }

                return roleList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: GetUsersInRoleAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: GetUsersInRoleAsync - {ex.Message}", ex);
            }
        }

        public async Task RevokeUserRoleMappingsAsync(string userUid)
        {
            /*
             * Bulk Revoke Logic (called by UserService on user soft-deletion):
             * -----------------------------------------------------------------
             * Soft-revokes all active role mappings for a given user so that the
             * deleted user no longer appears in GetUsersInRoleAsync results.
             * Records are kept for audit trail — they can be reactivated if the
             * deletion is ever reversed at the data level.
             */

            ArgumentNullException.ThrowIfNull(userUid);

            try
            {
                await _roleMappingRepository.RevokeAllMappingsByUserUidAsync(userUid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: RevokeUserRoleMappingsAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: RevokeUserRoleMappingsAsync - {ex.Message}", ex);
            }
        }

        public async Task<bool> IsUserInRoleAsync(string userUid, string roleTag, string contextId)
        {
            ArgumentNullException.ThrowIfNull(userUid);
            ArgumentNullException.ThrowIfNull(roleTag);

            try
            {
                return await _roleMappingRepository.IsUserInRoleAsync(userUid, roleTag, contextId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: IsUserInRoleAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: IsUserInRoleAsync - {ex.Message}", ex);
            }
        }

        // ── hospital-admin application ────────────────────────────────────────────

        public async Task<UserRequestDto> SubmitHospitalAdminApplicationAsync(string callerUserUid, string hospitalId, string? comment)
        {
            ArgumentNullException.ThrowIfNull(callerUserUid);
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
                if (foundHospital is null)
                    throw new Exception($"UserRoleMappingService: SubmitHospitalAdminApplicationAsync - hospital {hospitalId} not found");

                var request = await _userRequestRepository.CreateAsync(new UserRequestModel
                {
                    UserUid = callerUserUid,
                    RequestType = "HospitalAdminApplication",
                    TargetHospitalId = hospitalId,
                    Status = "Pending",
                    UserComment = comment,
                    CreatedAt = DateTime.UtcNow
                });

                await _notificationService.NotifyAllAdminsAsync(
                    $"A user has applied to become hospital-admin for {foundHospital.HospitalName}.", "/hospital");

                return MapToUserRequestDto(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: SubmitHospitalAdminApplicationAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: SubmitHospitalAdminApplicationAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserRequestDto> ApproveHospitalAdminApplicationAsync(int requestId, string callerUserUid, bool callerIsAdmin, string? comment)
        {
            try
            {
                if (!callerIsAdmin)
                    throw new UnauthorizedAccessException("UserRoleMappingService: ApproveHospitalAdminApplicationAsync - only a platform admin may approve this request");

                var request = await ValidatePendingHospitalAdminApplicationAsync(requestId);

                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(request.TargetHospitalId!);
                if (foundHospital is null)
                    throw new Exception($"UserRoleMappingService: ApproveHospitalAdminApplicationAsync - hospital {request.TargetHospitalId} not found");

                // Reuse the same assign logic used by the drag-and-drop UI — one code path, no duplication
                await AssignRoleAsync(new AssignRoleRequestDto
                {
                    UserUid = request.UserUid,
                    RoleTag = "hospital-admin",
                    ContextId = foundHospital.HospitalUid
                });

                request.Status = "Approved";
                request.AdminComment = comment;
                request.ResolvedAt = DateTime.UtcNow;
                request.ResolvedByUserUid = callerUserUid;
                var updated = await _userRequestRepository.UpdateAsync(request);

                await _notificationService.NotifyAsync(request.UserUid, $"Your application to become hospital-admin for {foundHospital.HospitalName} was approved.", "/user");

                return MapToUserRequestDto(updated);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: ApproveHospitalAdminApplicationAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: ApproveHospitalAdminApplicationAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserRequestDto> RejectHospitalAdminApplicationAsync(int requestId, string callerUserUid, bool callerIsAdmin, string? comment)
        {
            try
            {
                if (!callerIsAdmin)
                    throw new UnauthorizedAccessException("UserRoleMappingService: RejectHospitalAdminApplicationAsync - only a platform admin may reject this request");

                var request = await ValidatePendingHospitalAdminApplicationAsync(requestId);

                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(request.TargetHospitalId!);

                request.Status = "Rejected";
                request.AdminComment = comment;
                request.ResolvedAt = DateTime.UtcNow;
                request.ResolvedByUserUid = callerUserUid;
                var updated = await _userRequestRepository.UpdateAsync(request);

                await _notificationService.NotifyAsync(request.UserUid,
                    $"Your application to become hospital-admin for {foundHospital?.HospitalName ?? request.TargetHospitalId} was rejected.{(string.IsNullOrWhiteSpace(comment) ? "" : $" Reason: {comment}")}");

                return MapToUserRequestDto(updated);
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: RejectHospitalAdminApplicationAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: RejectHospitalAdminApplicationAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserRequestDto>> GetPendingRequestsAsync()
        {
            try
            {
                var pending = await _userRequestRepository.GetAllPendingAsync();

                var userUids = pending.Select(r => r.UserUid).Distinct().ToList();
                var users = await _userRepository.GetUsersByUserUidsAsync(userUids);
                var userByUid = users.ToDictionary(u => u.UserUid);

                return pending.Select(r =>
                {
                    userByUid.TryGetValue(r.UserUid, out var user);
                    return MapToUserRequestDto(r, user?.UserId, user?.UserName);
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserRoleMappingService: GetPendingRequestsAsync - {Message}", ex.Message);
                throw new Exception($"UserRoleMappingService: GetPendingRequestsAsync - {ex.Message}", ex);
            }
        }

        private async Task<UserRequestModel> ValidatePendingHospitalAdminApplicationAsync(int requestId)
        {
            var request = await _userRequestRepository.GetByIdAsync(requestId);
            if (request is null)
                throw new Exception($"UserRoleMappingService: request {requestId} not found");

            if (request.RequestType != "HospitalAdminApplication")
                throw new Exception($"UserRoleMappingService: request {requestId} is not a hospital-admin application");

            if (request.Status != "Pending")
                throw new Exception($"UserRoleMappingService: request {requestId} has already been resolved (status: {request.Status})");

            return request;
        }

        private static UserRequestDto MapToUserRequestDto(UserRequestModel request, string? userId = null, string? userName = null)
        {
            return new UserRequestDto
            {
                Id = request.Id,
                UserUid = request.UserUid,
                UserId = userId,
                UserName = userName,
                RequestType = request.RequestType,
                TargetHospitalId = request.TargetHospitalId,
                Status = request.Status,
                UserComment = request.UserComment,
                AdminComment = request.AdminComment,
                CreatedAt = request.CreatedAt,
                ResolvedAt = request.ResolvedAt,
                ResolvedByUserUid = request.ResolvedByUserUid
            };
        }

        // ── private mapping helpers ───────────────────────────────────────────────

        private static AssignRoleResponseDto MapToAssignRoleResponseDto(UserRoleMappingModel mapping)
        {
            return new AssignRoleResponseDto
            {
                Id = mapping.Id,
                UserUid = mapping.UserUid,
                RoleTag = mapping.RoleTag,
                ContextId = mapping.ContextId,
                IsActive = mapping.IsActive,
                CreatedAt = mapping.CreatedAt
            };
        }

        private static UserRoleMappingProfileDto MapToProfileDto(UserRoleMappingModel mapping, string? userId = null, string? userName = null)
        {
            return new UserRoleMappingProfileDto
            {
                Id = mapping.Id,
                UserUid = mapping.UserUid,
                UserId = userId,
                UserName = userName,
                RoleTag = mapping.RoleTag,
                ContextId = mapping.ContextId,
                IsActive = mapping.IsActive,
                CreatedAt = mapping.CreatedAt,
                UpdatedAt = mapping.UpdatedAt
            };
        }
    }
}
