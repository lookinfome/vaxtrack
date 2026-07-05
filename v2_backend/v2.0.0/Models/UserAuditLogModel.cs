using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vaxtrack.Models
{
    public class UserAuditLogModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string UserId { get; set; } = "";   // logical FK -> UserModel.UserId

        // "Disabled" | "ReactivationRequested" | "ReactivationApproved" | "ReactivationRejected"
        public string ActionType { get; set; } = "";

        public string ActorUserUid { get; set; } = "";
        public string ActorRole { get; set; } = "";     // "admin" | "user" (self, for reactivation requests)
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
