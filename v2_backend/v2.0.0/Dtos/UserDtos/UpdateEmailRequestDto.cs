using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.UserDtos
{
    public class UpdateEmailRequestDto
    {
        [Required(ErrorMessage = "user id is required")]
        public string UserId { get; set; } = "";

        [Required(ErrorMessage = "new email is required")]
        [EmailAddress(ErrorMessage = "invalid email address")]
        public string NewEmail { get; set; } = "";

        // Required when caller is not admin — proves identity before changing a sensitive credential
        public string CurrentPassword { get; set; } = "";
    }
}
