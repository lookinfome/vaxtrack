using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.AuthDtos
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "email is required")]
        [EmailAddress(ErrorMessage = "invalid email address")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "password is required")]
        public string Password { get; set; } = "";
    }
}
