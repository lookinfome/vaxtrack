using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;
using Vaxtrack.Dtos.UserDtos;
using Vaxtrack.Interfaces.UtilityInterfaces;
using BC = BCrypt.Net.BCrypt;

namespace Vaxtrack.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserCredentialsRepository _credentialsRepository;
        private readonly IBookingService _bookingService;
        private readonly IUserRoleMappingService _roleMappingService;
        private readonly IUtilityService _utilityService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IUserCredentialsRepository credentialsRepository,
            IBookingService bookingService,
            IUserRoleMappingService roleMappingService,
            IUtilityService utilityService,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _credentialsRepository = credentialsRepository;
            _bookingService = bookingService;
            _roleMappingService = roleMappingService;
            _utilityService = utilityService;
            _logger = logger;
        }

        public async Task<CreateUserResponseDto> CreateUserAsync(CreateUserRequestDto createUserRequestDto)
        {
            /*
             * Create Logic:
             * -------------
             * Registers a new user. Profile (UserModel) and credentials (UserCredentialsModel)
             * are saved to separate tables. UserUid is the link between them.
             * UserId (readable) and UserUid (GUID) are system-generated.
             * UserRole defaults to false (regular user) — role elevation is admin-only.
             * Age is calculated at registration and stored; not recalculated on future reads.
             *
             * Edge cases blocked:
             *   - Null request       → ArgumentNullException before entering try.
             *   - Future birthdate   → throws (would produce a negative age).
             *   - Duplicate email    → throws (email must be unique across active accounts).
             */

            ArgumentNullException.ThrowIfNull(createUserRequestDto);

            try
            {
                if (createUserRequestDto.UserBirthdate >= DateTime.UtcNow)
                    throw new Exception($"UserService: CreateUserAsync - birth date cannot be today or in the future");

                // Reject if email is already registered to an active account
                var existingCredentials = await _credentialsRepository.GetCredentialsByEmailAsync(createUserRequestDto.Email);
                if (existingCredentials is not null)
                    throw new Exception($"UserService: CreateUserAsync - email {createUserRequestDto.Email} is already in use");

                var newUser = await MapUserCreateRequestToUserModel(createUserRequestDto);
                var createdUser = await _userRepository.CreateUserAsync(newUser);

                // Save credentials in the dedicated table, linked by UserUid
                var timestamp = DateTime.UtcNow;
                var credentials = new UserCredentialsModel
                {
                    UserUid      = createdUser.UserUid,
                    Email        = createUserRequestDto.Email,
                    PasswordHash = BC.HashPassword(createUserRequestDto.Password),
                    CreatedAt    = timestamp,
                    UpdatedAt    = timestamp
                };
                await _credentialsRepository.CreateCredentialsAsync(credentials);

                return MapToCreateUserResponseDto(createdUser, createUserRequestDto.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: CreateUserAsync - {Message}", ex.Message);
                throw new Exception($"UserService: CreateUserAsync - {ex.Message}", ex);
            }
        }

        public async Task<UpdateUserResponseDto> UpdateUserAsync(UpdateUserRequestDto updateUserRequestDto, string callerUserUid, bool callerIsAdmin)
        {
            /*
             * Update Logic:
             * -------------
             * Updates a user's mutable profile fields. The following fields are mutable:
             *   FirstName, LastName, UserGender, UserPhone, UserAddress, UserPinCode, ProfilePicturePath.
             *
             * The following fields are intentionally immutable via this method:
             *   UserId (primary key), UserUid (system GUID), UserBirthdate, UserAge, UserRole,
             *   CreatedAt. Email and PasswordHash live in UserCredentials — use UpdateEmailAsync /
             *   ChangePasswordAsync for those.
             *
             * Ownership: only the account owner or an admin may update a profile.
             *
             * Edge cases blocked:
             *   - Null request         → ArgumentNullException before entering try.
             *   - User not found       → throws (includes soft-deleted users).
             *   - Caller not the owner → throws UnauthorizedAccessException.
             */

            ArgumentNullException.ThrowIfNull(updateUserRequestDto);

            try
            {
                string userId = updateUserRequestDto.UserId;
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(userId);

                if (foundUser is null)
                    throw new Exception($"UserService: UpdateUserAsync - user {userId} not found");

                if (!callerIsAdmin && foundUser.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException($"UserService: UpdateUserAsync - caller does not own account {userId}");

                var mappedUser = MapUserUpdateRequestToUserModel(foundUser, updateUserRequestDto);
                var updatedUser = await _userRepository.UpdateUserAsync(mappedUser);
                return MapToUpdateUserResponseDto(updatedUser);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: UpdateUserAsync - {Message}", ex.Message);
                throw new Exception($"UserService: UpdateUserAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserProfileDataDto> GetUserProfileDataAsync(string userId, string callerUserUid, bool callerIsAdmin)
        {
            /*
             * Ownership: only the account owner or an admin may view a full profile.
             * This prevents any authenticated user from harvesting other users' PII.
             */

            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(userId);

                if (foundUser is null)
                    throw new Exception($"UserService: GetUserProfileDataAsync - user {userId} not found");

                if (!callerIsAdmin && foundUser.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException($"UserService: GetUserProfileDataAsync - caller does not own account {userId}");

                var credentials = await _credentialsRepository.GetCredentialsByUserUidAsync(foundUser.UserUid);
                return MapToUserProfileDto(foundUser, credentials?.Email ?? "");
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: GetUserProfileDataAsync - {Message}", ex.Message);
                throw new Exception($"UserService: GetUserProfileDataAsync - {ex.Message}", ex);
            }
        }

        public async Task<List<UserProfileDataDto>> GetAllUsersAsync()
        {
            try
            {
                var foundUsersList = await _userRepository.GetAllUsersDetailAsync() ?? [];

                // Bulk-fetch credentials in one query to avoid N+1 queries
                var userUids = foundUsersList.Select(u => u.UserUid).ToList();
                var credentialsList = await _credentialsRepository.GetCredentialsByUserUidsAsync(userUids);
                var emailByUid = credentialsList.ToDictionary(c => c.UserUid, c => c.Email);

                List<UserProfileDataDto> usersList = [];
                foreach (var user in foundUsersList)
                {
                    emailByUid.TryGetValue(user.UserUid, out string? email);
                    usersList.Add(MapToUserProfileDto(user, email ?? ""));
                }

                return usersList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: GetAllUsersAsync - {Message}", ex.Message);
                throw new Exception($"UserService: GetAllUsersAsync - {ex.Message}", ex);
            }
        }

        public async Task<UpdateEmailResponseDto> UpdateEmailAsync(UpdateEmailRequestDto updateEmailRequestDto, string callerUserUid, bool callerIsAdmin)
        {
            /*
             * Email Update Logic:
             * -------------------
             * Changes the email stored in UserCredentials. Because email is a login credential,
             * the caller must supply their current password to confirm identity (unless the caller
             * is an admin, who can override without knowing the user's password — e.g. for support).
             *
             * Edge cases blocked:
             *   - Null request           → ArgumentNullException.
             *   - User not found         → throws.
             *   - Caller not owner       → throws UnauthorizedAccessException.
             *   - Wrong current password → throws (non-admin callers only).
             *   - New email already used → throws (must be unique across active accounts).
             */

            ArgumentNullException.ThrowIfNull(updateEmailRequestDto);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(updateEmailRequestDto.UserId);

                if (foundUser is null)
                    throw new Exception($"UserService: UpdateEmailAsync - user {updateEmailRequestDto.UserId} not found");

                if (!callerIsAdmin && foundUser.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException($"UserService: UpdateEmailAsync - caller does not own account {updateEmailRequestDto.UserId}");

                var currentCredentials = await _credentialsRepository.GetCredentialsByUserUidAsync(foundUser.UserUid);

                if (currentCredentials is null)
                    throw new Exception($"UserService: UpdateEmailAsync - credentials not found for user {updateEmailRequestDto.UserId}");

                // Non-admins must verify their current password before changing a login credential
                if (!callerIsAdmin && !BC.Verify(updateEmailRequestDto.CurrentPassword, currentCredentials.PasswordHash))
                    throw new Exception($"UserService: UpdateEmailAsync - current password is incorrect");

                // New email must not already belong to an active account
                var conflicting = await _credentialsRepository.GetCredentialsByEmailAsync(updateEmailRequestDto.NewEmail);
                if (conflicting is not null && conflicting.UserUid != foundUser.UserUid)
                    throw new Exception($"UserService: UpdateEmailAsync - email {updateEmailRequestDto.NewEmail} is already in use");

                await _credentialsRepository.UpdateEmailAsync(foundUser.UserUid, updateEmailRequestDto.NewEmail);

                foundUser.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(foundUser);

                return new UpdateEmailResponseDto
                {
                    UserId    = foundUser.UserId,
                    NewEmail  = updateEmailRequestDto.NewEmail,
                    UpdatedAt = foundUser.UpdatedAt
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: UpdateEmailAsync - {Message}", ex.Message);
                throw new Exception($"UserService: UpdateEmailAsync - {ex.Message}", ex);
            }
        }

        public async Task<ChangePasswordResponseDto> ChangePasswordAsync(ChangePasswordRequestDto changePasswordRequestDto, string callerUserUid, bool callerIsAdmin)
        {
            /*
             * Password Change Logic:
             * ----------------------
             * Hashes the new password with BCrypt and updates UserCredentials.
             * Non-admin callers must supply their current password to confirm identity.
             * Admin callers can reset any user's password without knowing the current one
             * (support / account recovery use case).
             *
             * Edge cases blocked:
             *   - Null request           → ArgumentNullException.
             *   - User not found         → throws.
             *   - Caller not owner       → throws UnauthorizedAccessException.
             *   - Wrong current password → throws (non-admin callers only).
             *   - New == old password    → allowed (idempotent, no business rule prevents it).
             */

            ArgumentNullException.ThrowIfNull(changePasswordRequestDto);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(changePasswordRequestDto.UserId);

                if (foundUser is null)
                    throw new Exception($"UserService: ChangePasswordAsync - user {changePasswordRequestDto.UserId} not found");

                if (!callerIsAdmin && foundUser.UserUid != callerUserUid)
                    throw new UnauthorizedAccessException($"UserService: ChangePasswordAsync - caller does not own account {changePasswordRequestDto.UserId}");

                var currentCredentials = await _credentialsRepository.GetCredentialsByUserUidAsync(foundUser.UserUid);

                if (currentCredentials is null)
                    throw new Exception($"UserService: ChangePasswordAsync - credentials not found for user {changePasswordRequestDto.UserId}");

                // Non-admins must verify their current password
                if (!callerIsAdmin && !BC.Verify(changePasswordRequestDto.CurrentPassword, currentCredentials.PasswordHash))
                    throw new Exception($"UserService: ChangePasswordAsync - current password is incorrect");

                var newHash = BC.HashPassword(changePasswordRequestDto.NewPassword);
                await _credentialsRepository.UpdatePasswordHashAsync(foundUser.UserUid, newHash);

                foundUser.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(foundUser);

                return new ChangePasswordResponseDto
                {
                    UserId    = foundUser.UserId,
                    UpdatedAt = foundUser.UpdatedAt
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: ChangePasswordAsync - {Message}", ex.Message);
                throw new Exception($"UserService: ChangePasswordAsync - {ex.Message}", ex);
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            /*
             * Admin-initiated delete by UserId.
             * Delegates cascade logic to ExecuteUserDeleteCascadeAsync.
             */

            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(userId);

                if (foundUser is null)
                    throw new Exception($"UserService: DeleteUserAsync - user {userId} not found");

                await ExecuteUserDeleteCascadeAsync(foundUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: DeleteUserAsync - {Message}", ex.Message);
                throw new Exception($"UserService: DeleteUserAsync - {ex.Message}", ex);
            }
        }

        public async Task DeleteMyAccountAsync(string callerUserUid)
        {
            /*
             * Self-delete: the caller deletes their own account using the UserUid from their JWT.
             * Delegates the same cascade as admin delete — no difference in what gets cleaned up.
             */

            ArgumentNullException.ThrowIfNull(callerUserUid);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserUidAsync(callerUserUid);

                if (foundUser is null)
                    throw new Exception($"UserService: DeleteMyAccountAsync - account not found for caller");

                await ExecuteUserDeleteCascadeAsync(foundUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: DeleteMyAccountAsync - {Message}", ex.Message);
                throw new Exception($"UserService: DeleteMyAccountAsync - {ex.Message}", ex);
            }
        }

        // ── private helpers ───────────────────────────────────────────────────────

        private async Task ExecuteUserDeleteCascadeAsync(UserModel user)
        {
            /*
             * Shared cascade delete logic:
             *   1. Soft-delete user profile.
             *   2. Soft-delete all bookings → restores pending vaccine slots back to hospitals.
             *   3. Revoke all role mappings.
             *   4. Soft-delete credentials → clears PasswordHash; email freed for re-registration.
             *
             * Login is blocked at step 1 (credentials lookup returns null for IsDeleted rows) and
             * also at step 2 (user profile lookup excludes IsDeleted rows) — double-gated.
             */

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            await _bookingService.DeleteBookingsByUserUidAsync(user.UserUid);
            await _roleMappingService.RevokeUserRoleMappingsAsync(user.UserUid);
            await _credentialsRepository.SoftDeleteCredentialsByUserUidAsync(user.UserUid);
        }

        private async Task<UserModel> MapUserCreateRequestToUserModel(CreateUserRequestDto createUserRequestDto)
        {
            var timestamp = DateTime.UtcNow;
            var guid = await _utilityService.GenerateGuidAsync();
            var uniqueId = await _utilityService.GenerateUniqueIdAsync(createUserRequestDto.FirstName);
            var age = await _utilityService.CalculateAgeAsync(createUserRequestDto.UserBirthdate);

            return new UserModel
            {
                UserId = uniqueId,
                UserName = $"{createUserRequestDto.FirstName} {createUserRequestDto.LastName}".Trim(),
                UserBirthdate = createUserRequestDto.UserBirthdate,
                UserAge = age,
                UserUid = guid,
                UserGender = createUserRequestDto.UserGender,
                UserPhone = createUserRequestDto.UserPhone,
                UserAddress = createUserRequestDto.UserAddress,
                UserPinCode = createUserRequestDto.UserPinCode,
                UserRole = false,
                ProfilePicturePath = "",
                CreatedAt = timestamp,
                UpdatedAt = null   // null until first profile update via UpdateUserAsync
            };
        }

        private static UserModel MapUserUpdateRequestToUserModel(UserModel existingUser, UpdateUserRequestDto updateUserRequestDto)
        {
            existingUser.UserName = $"{updateUserRequestDto.FirstName} {updateUserRequestDto.LastName}".Trim();
            existingUser.UserGender = updateUserRequestDto.UserGender;
            existingUser.UserPhone = updateUserRequestDto.UserPhone;
            existingUser.ProfilePicturePath = updateUserRequestDto.ProfilePicturePath;
            existingUser.UserAddress = updateUserRequestDto.UserAddress;
            existingUser.UserPinCode = updateUserRequestDto.UserPinCode;
            existingUser.UpdatedAt = DateTime.UtcNow;
            return existingUser;
        }

        private static CreateUserResponseDto MapToCreateUserResponseDto(UserModel user, string email)
        {
            return new CreateUserResponseDto
            {
                UserId    = user.UserId,
                UserName  = user.UserName,
                Email     = email,
                UserRole  = user.UserRole,
                CreatedAt = user.CreatedAt
            };
        }

        private static UpdateUserResponseDto MapToUpdateUserResponseDto(UserModel user)
        {
            return new UpdateUserResponseDto
            {
                UserId             = user.UserId,
                FirstName          = user.UserName.Split(' ')[0],
                LastName           = user.UserName.Contains(' ') ? user.UserName.Split(' ')[1] : "",
                UserGender         = user.UserGender,
                UserPhone          = user.UserPhone,
                UserAddress        = user.UserAddress,
                UserPinCode        = user.UserPinCode,
                ProfilePicturePath = user.ProfilePicturePath,
                UpdatedAt          = user.UpdatedAt
            };
        }

        private static UserProfileDataDto MapToUserProfileDto(UserModel user, string email)
        {
            return new UserProfileDataDto
            {
                UserId             = user.UserId,
                UserName           = user.UserName,
                Email              = email,
                UserBirthdate      = user.UserBirthdate,
                UserAge            = user.UserAge,
                UserGender         = user.UserGender,
                UserPhone          = user.UserPhone,
                UserRole           = user.UserRole,
                UserAddress        = user.UserAddress,
                UserPinCode        = user.UserPinCode,
                ProfilePicturePath = user.ProfilePicturePath,
                CreatedAt          = user.CreatedAt,
                UpdatedAt          = user.UpdatedAt
            };
        }
    }
}
