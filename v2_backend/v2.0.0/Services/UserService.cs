using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Models;
using Vaxtrack.Dtos.UserDtos;

namespace Vaxtrack.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<CreateUserResponseDto> CreateUserAsync(CreateUserRequestDto createUserRequestDto)
        {
            ArgumentNullException.ThrowIfNull(createUserRequestDto);

            try
            {
                var newUser = MapUserCreateRequestToUserModel(createUserRequestDto);
                var createdUser = await _userRepository.CreateUserAsync(newUser);

                return MapToCreateUserResponseDto(createdUser);
            }
            catch(Exception ex)
            {
                throw new Exception($"UserService: CreateUserAsync - {ex}");
            }
        }
        public async Task<UpdateUserResponseDto> UpdateUserAsync(UpdateUserRequestDto updateUserRequestDto)
        {
            ArgumentNullException.ThrowIfNull(updateUserRequestDto);

            try
            {
                string userId = updateUserRequestDto.UserId;
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(userId);

                if(foundUser == null)
                {
                    throw new Exception($"UserService: UpdateUserAsync - user {userId} not found!");
                }

                var mapFoundUser = MapUserUpdateRequestToUserModel(foundUser, updateUserRequestDto);
                var updatedUser = await _userRepository.UpdateUserAsync(mapFoundUser);

                return MapToUpdateUserResponseDto(updatedUser);
            }
            catch(Exception ex)
            {
                throw new Exception($"{ex}");
            }
        }
        public async Task<UserProfileDataDto> GetUserProfileDataAsync(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(userId);
                if(foundUser == null)
                {
                    throw new Exception($"UserService: GetUserProfileDataAsync - user {userId} not found!");                
                }

                var mapFoundUser = MapToUserProfileDto(foundUser);
                return mapFoundUser;
            }
            catch(Exception ex)
            {
                throw new Exception($"{ex}");
            }
        }
        public async Task<List<UserProfileDataDto>> GetAllUsersAsync()
        {
            try
            {
                var foundUsersList = await _userRepository.GetAllUsersDetailAsync();
                if(foundUsersList == null)
                {
                    throw new Exception($"UserService: GetAllUsersAsync - no users found!");
                }

                List<UserProfileDataDto> usersList = new List<UserProfileDataDto>();

                foreach (var user in foundUsersList)
                {
                    usersList.Add(MapToUserProfileDto(user));
                }

                return usersList;
            }
            catch(Exception ex)
            {
                throw new Exception($"{ex}");
            }


        }
        public async Task DeleteUserAsync(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                var foundUser = await _userRepository.GetUserDetailsByUserIdAsync(userId);
                if(foundUser == null)
                {
                    throw new Exception($"UserService: DeleteUserAsync - user {userId} not found!");
                }

                var mapFoundUser = MapUserUpdateRequestToUserModel(foundUser, null, true);
                await _userRepository.UpdateUserAsync(mapFoundUser);
            }
            catch(Exception ex)
            {
                throw new Exception($"{ex}");
            }

        }

        // utitlity methods

        private UserModel MapUserCreateRequestToUserModel(CreateUserRequestDto createUserRequestDto)
        {
            var timestamp = DateTime.UtcNow;

            return new UserModel
            {
                UserId = GenerateUserId(createUserRequestDto.FirstName),
                UserName = $"{createUserRequestDto.FirstName} {createUserRequestDto.LastName}".Trim(),
                UserBirthdate = createUserRequestDto.UserBirthdate,
                UserAge = CalculateAge(createUserRequestDto.UserBirthdate),
                UserUid = Guid.NewGuid().ToString(),
                UserGender = createUserRequestDto.UserGender,
                UserPhone = createUserRequestDto.UserPhone,
                UserAddress = createUserRequestDto.UserAddress,
                UserPinCode = createUserRequestDto.UserPinCode,
                UserRole = false, // default role for new users is set to false (non-admin)
                ProfilePicturePath = "",
                CreatedAt = timestamp,
                UpdatedAt = timestamp

            };
        }
        private UserModel MapUserUpdateRequestToUserModel(UserModel existingUser, UpdateUserRequestDto? updateUserRequestDto = null, bool? isDelete = null)
        {
            var timestamp = DateTime.UtcNow;

            // Handle deletion logic
            if(isDelete != null)
            {
                existingUser.DeletedAt = timestamp;
                existingUser.IsDeleted = isDelete.Value;
            }

            // Handle update logic (only if updateUserRequestDto is provided)
            if(updateUserRequestDto != null && isDelete == null)
            {
                existingUser.UserName = $"{updateUserRequestDto.FirstName} {updateUserRequestDto.LastName}".Trim();
                existingUser.UserGender = updateUserRequestDto.UserGender;
                existingUser.UserPhone = updateUserRequestDto.UserPhone;
                existingUser.ProfilePicturePath = updateUserRequestDto.ProfilePicturePath;
                existingUser.UserAddress = updateUserRequestDto.UserAddress;
                existingUser.UserPinCode = updateUserRequestDto.UserPinCode;
            }

            existingUser.UpdatedAt = timestamp;

            return existingUser;
        }        
        private CreateUserResponseDto MapToCreateUserResponseDto(UserModel user)
        {
            return new CreateUserResponseDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserRole = user.UserRole,
                CreatedAt = user.CreatedAt
            };
        }
        private UpdateUserResponseDto MapToUpdateUserResponseDto(UserModel user)
        {
            return new UpdateUserResponseDto
            {
                UserId = user.UserId,
                FirstName = user.UserName.Split(' ')[0], // Assuming the first part of the name is the first name
                LastName = user.UserName.Contains(' ') ? user.UserName.Split(' ')[1] : "", // Assuming the second part of the name is the last name
                UserGender = user.UserGender,
                UserPhone = user.UserPhone,
                UserAddress = user.UserAddress,
                UserPinCode = user.UserPinCode,
                ProfilePicturePath = user.ProfilePicturePath,
                UpdatedAt = user.UpdatedAt
            };
        }
        private UserProfileDataDto MapToUserProfileDto(UserModel user)
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
        private int CalculateAge(DateTime userBirthdate)
        {
            var today = DateTime.Today;
            var age = today.Year - userBirthdate.Year;

            if (userBirthdate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
        private string GenerateUserId(string firstName)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 6); // 6 characters from a GUID for uniqueness
            var userId = $"{firstName}-{timestamp.ToString().Substring(0, 2)}-{randomPart.ToString().Substring(0, 2)}";
            return userId;
        }

    }
}
