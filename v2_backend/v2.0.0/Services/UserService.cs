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

            var newUser = MapNewUserToUserModel(createUserRequestDto);
            var savedUser = await _userRepository.AddUserAsync(newUser);
            return MapToCreateUserResponseDto(savedUser);
        }

        public async Task<UpdateUserResponseDto> UpdateUserAsync(UpdateUserRequestDto updateUserRequestDto)
        {
            ArgumentNullException.ThrowIfNull(updateUserRequestDto);
            if(!await _userRepository.UserExistsAsync(updateUserRequestDto.UserId))
            {
                throw new ArgumentException($"userservice: updateuser - user with id {updateUserRequestDto.UserId} not found");
            }

            var foundUser = await _userRepository.GetUserByIdAsync(updateUserRequestDto.UserId);
            var updatedUser = MapUpdateUserRequestDtoToUserModel(updateUserRequestDto, foundUser!);
            var savedUser = await _userRepository.UpdateUserAsync(updatedUser);
            return MapToUpdateUserResponseDto(savedUser);
        }

        public async Task<UserProfileDataDto> GetUserProfileDataAsync(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);
            if(!await _userRepository.UserExistsAsync(userId))
            {
                throw new ArgumentException($"userservice: getuserprofiledata - user with id {userId} not found");
            }

            var foundUser = await _userRepository.GetUserByIdAsync(userId);

            return MapToUserProfileDto(foundUser!);
        }

        public async Task<List<UserProfileDataDto>> GetAllUsersAsync()
        {
            var allUsers = await _userRepository.GetAllUsersAsync();

            ArgumentNullException.ThrowIfNull(allUsers);

            List<UserProfileDataDto> usersList = new List<UserProfileDataDto>();
            foreach (var user in allUsers)
            {
                usersList.Add(MapToUserProfileDto(user));
            }    
            return usersList;
        }
        public async Task DeleteUserAsync(string userId)
        {
            ArgumentNullException.ThrowIfNull(userId);
            if (!await _userRepository.UserExistsAsync(userId))
            {
                throw new ArgumentException($"userservice: deleteuser - user with id {userId} not found");
            }

            var timestamp = DateTime.UtcNow;
            await _userRepository.DeleteUserAsync(userId, timestamp, true);
        }

        // utitlity methods

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
            ArgumentNullException.ThrowIfNull(firstName);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomPart = Guid.NewGuid().ToString("N").Substring(0, 6); // 6 characters from a GUID for uniqueness
            var userId = $"{firstName}-{timestamp.ToString().Substring(0, 2)}-{randomPart.ToString().Substring(0, 2)}";
            return userId;
        }

        private UserModel MapNewUserToUserModel(CreateUserRequestDto createUserRequestDto)
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

        private UserModel MapUpdateUserRequestDtoToUserModel(UpdateUserRequestDto updateUserRequestDto, UserModel existingUser)
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
    }
}
