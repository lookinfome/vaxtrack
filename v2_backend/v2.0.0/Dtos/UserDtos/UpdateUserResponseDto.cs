using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.UserDtos
{
    public class UpdateUserResponseDto
    {
        public string UserId { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string UserGender { get; set; } = "";
        public string UserPhone { get; set; } = "";
        public string UserAddress {get; set;} = "";
        public string UserPinCode { get; set; } = "";
        public string ProfilePicturePath { get; set; } = "";
        public DateTime? UpdatedAt { get; set; }
    }
}
