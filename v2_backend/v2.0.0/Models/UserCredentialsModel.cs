using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vaxtrack.Models
{
    public class UserCredentialsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // FK to UserModel.UserUid — links credentials to the user profile
        public string UserUid { get; set; } = "";

        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";  // BCrypt hash only; plain text never stored

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
