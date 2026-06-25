using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vaxtrack.Models
{
    public class PasswordResetTokenModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string UserUid { get; set; } = "";   // FK → UserModel.UserUid
        public string Token { get; set; } = "";     // secure random token sent to the user

        public bool IsUsed { get; set; } = false;   // single-use: marked true immediately after reset

        public DateTime ExpiresAt { get; set; }     // 15 minutes from creation
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }       // null until the token is consumed
    }
}
