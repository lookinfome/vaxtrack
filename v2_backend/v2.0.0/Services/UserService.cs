using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;
using Vaxtrack.Dtos.UserDtos;
using Vaxtrack.Interfaces.UtilityInterfaces;

namespace Vaxtrack.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUtilityService _utilityService;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, IUtilityService utilityService, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _utilityService = utilityService;
            _logger = logger;
        }

        public async Task<CreateUserResponseDto> CreateUserAsync(CreateUserRequestDto createUserRequestDto)
        {
            /*
             * Create Logic:
             * -------------
             * Registers a new user. UserId (readable) and UserUid (GUID) are system-generated.
             * UserRole defaults to false (regular user) — role elevation is an admin-only operation
             * not exposed through this endpoint.
             * Age is calculated from UserBirthdate at registration time and stored; it is NOT
             * recalculated on future reads.
             *
             * Edge cases blocked:
             *   - Null request                → ArgumentNullException thrown before entering try.
             *   - Future birthdate            → throws (would produce a negative age).
             */

            ArgumentNullException.ThrowIfNull(createUserRequestDto);

            try
            {
                if (createUserRequestDto.UserBirthdate >= DateTime.UtcNow)
                    throw new Exception($"UserService: CreateUserAsync - birth date cannot be today or in the future");

                var newUser = await MapUserCreateRequestToUserModel(createUserRequestDto);
                var createdUser = await _userRepository.CreateUserAsync(newUser);
                return MapToCreateUserResponseDto(createdUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: CreateUserAsync - {Message}", ex.Message);
                throw new Exception($"UserService: CreateUserAsync - {ex.Message}", ex);
            }
        }

        public async Task<UpdateUserResponseDto> UpdateUserAsync(UpdateUserRequestDto updateUserRequestDto)
        {
            /*
             * Update Logic:
             * -------------
             * Updates a user's mutable profile fields. The following fields are mutable:
             *   FirstName, LastName, UserGender, UserPhone, UserAddress, UserPinCode, ProfilePicturePath.
             *
             * The following fields are intentionally immutable via this method:
             *   UserId (primary key), UserUid (system GUID), UserBirthdate, UserAge, UserRole,
             *   CreatedAt.
             *
             * Edge cases blocked:
             *   - Null request       → ArgumentNullException thrown before entering try.
             *   - User not found     → throws (includes soft-deleted users, which are excluded from lookup).
             */

            ArgumentNullException.ThrowIfNull(updateUserRequestDto);

            try
            {
                string userId = updateUserRequestDto.UserId;
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(userId);

                if (foundUser is null)
                    throw new Exception($"UserService: UpdateUserAsync - user {userId} not found");

                var mappedUser = MapUserUpdateRequestToUserModel(foundUser, updateUserRequestDto);
                var updatedUser = await _userRepository.UpdateUserAsync(mappedUser);
                return MapToUpdateUserResponseDto(updatedUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: UpdateUserAsync - {Message}", ex.Message);
                throw new Exception($"UserService: UpdateUserAsync - {ex.Message}", ex);
            }
        }

        public async Task<UserProfileDataDto> GetUserProfileDataAsync(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(userId);

                if (foundUser is null)
                    throw new Exception($"UserService: GetUserProfileDataAsync - user {userId} not found");

                return MapToUserProfileDto(foundUser);
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
                var foundUsersList = await _userRepository.GetAllUsersDetailAsync();

                if (foundUsersList is null)
                    throw new Exception("UserService: GetAllUsersAsync - no users found");

                List<UserProfileDataDto> usersList = [];
                foreach (var user in foundUsersList)
                    usersList.Add(MapToUserProfileDto(user));

                return usersList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: GetAllUsersAsync - {Message}", ex.Message);
                throw new Exception($"UserService: GetAllUsersAsync - {ex.Message}", ex);
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            /*
             * Delete Logic:
             * -------------
             * Soft-deletes a user by setting IsDeleted = true and recording DeletedAt.
             * The record remains in the database but is excluded from all future lookups.
             * Once deleted, any subsequent call with the same userId returns "not found"
             * because the repository filters out soft-deleted records.
             *
             * Edge cases blocked:
             *   - Null userId        → ArgumentNullException thrown before entering try.
             *   - User not found     → throws (includes already-deleted users).
             *
             * Note: active bookings belonging to this user are NOT checked before deletion.
             * Those booking records will remain and will still reference the deleted user's UserUid.
             */

            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(userId);

                if (foundUser is null)
                    throw new Exception($"UserService: DeleteUserAsync - user {userId} not found");

                foundUser.IsDeleted = true;
                foundUser.DeletedAt = DateTime.UtcNow;
                foundUser.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(foundUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: DeleteUserAsync - {Message}", ex.Message);
                throw new Exception($"UserService: DeleteUserAsync - {ex.Message}", ex);
            }
        }

        // ── private mapping helpers ───────────────────────────────────────────────

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
                UpdatedAt = timestamp
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

        private static CreateUserResponseDto MapToCreateUserResponseDto(UserModel user)
        {
            return new CreateUserResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserRole = user.UserRole,
                CreatedAt = user.CreatedAt
            };
        }

        private static UpdateUserResponseDto MapToUpdateUserResponseDto(UserModel user)
        {
            return new UpdateUserResponseDto
            {
                UserId = user.UserId,
                FirstName = user.UserName.Split(' ')[0],
                LastName = user.UserName.Contains(' ') ? user.UserName.Split(' ')[1] : "",
                UserGender = user.UserGender,
                UserPhone = user.UserPhone,
                UserAddress = user.UserAddress,
                UserPinCode = user.UserPinCode,
                ProfilePicturePath = user.ProfilePicturePath,
                UpdatedAt = user.UpdatedAt
            };
        }

        private static UserProfileDataDto MapToUserProfileDto(UserModel user)
        {
            return new UserProfileDataDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserBirthdate = user.UserBirthdate,
                UserAge = user.UserAge,
                UserGender = user.UserGender,
                UserPhone = user.UserPhone,
                UserRole = user.UserRole,
                UserAddress = user.UserAddress,
                UserPinCode = user.UserPinCode,
                ProfilePicturePath = user.ProfilePicturePath,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
