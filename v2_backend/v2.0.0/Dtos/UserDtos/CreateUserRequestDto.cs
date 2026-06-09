using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.UserDtos
{
    public class CreateUserRequestDto
    {
        [Required(ErrorMessage = "user first name is required")]
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "user birth date is required")]
        [DataType(DataType.Date)]
        public DateTime UserBirthdate { get; set; }

        [Required(ErrorMessage = "user gender is required")]
        public string UserGender { get; set; } = "";

        [Required(ErrorMessage = "user phone no. is required")]
        [Phone(ErrorMessage = "invalid phone number")]
        [StringLength(10, ErrorMessage = "phone number cannot be longer than 10 digits")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "phone number must be 10 digits")]
        public string UserPhone { get; set; } = "";

        [Required(ErrorMessage = "user address is required")]
        public string UserAddress { get; set; } = "";

        [Required(ErrorMessage = "user address pincode is required")]        
        [StringLength(10, ErrorMessage = "pin code cannot be longer than 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "pin code must be 6 digits")]
        public string UserPinCode { get; set; } = "";

    }
}
