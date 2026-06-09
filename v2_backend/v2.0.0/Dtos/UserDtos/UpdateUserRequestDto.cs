using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.UserDtos
{
    public class UpdateUserRequestDto
    {
        [Required(ErrorMessage = "user id is required")]
        public string UserId { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public string UserGender { get; set; } = "";

        [Phone(ErrorMessage = "invalid phone number")]
        [StringLength(10, ErrorMessage = "phone number cannot be longer than 10 digits")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "phone number must be 10 digits")]
        public string UserPhone { get; set; } = "";

        public string UserAddress { get; set; } = "";
  
        [StringLength(10, ErrorMessage = "pin code cannot be longer than 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "pin code must be 6 digits")]
        public string UserPinCode { get; set; } = "";

        public string ProfilePicturePath { get; set; } = "";

    }
}
