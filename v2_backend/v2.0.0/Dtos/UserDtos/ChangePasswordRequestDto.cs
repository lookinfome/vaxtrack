using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.UserDtos
{
    public class ChangePasswordRequestDto
    {
        [Required(ErrorMessage = "user id is required")]
        public string UserId { get; set; } = "";

        // Required when caller is not admin; admin can reset without knowing the old password
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "new password is required")]
        [MinLength(8, ErrorMessage = "password must be at least 8 characters")]
        public string NewPassword { get; set; } = "";
    }
}
