using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vaxtrack.Models
{
    public class UserRoleMappingModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // References UserModel.UserUid — the user this role is assigned to
        public string UserUid { get; set; } = "";

        // Role tag e.g. "hospital-admin", "booking-approver"
        public string RoleTag { get; set; } = "";

        // Scope of the role — empty string means global/platform-wide;
        // non-empty means entity-scoped (e.g. HospitalId for "hospital-admin")
        public string ContextId { get; set; } = "";

        // Soft-revoke: false means the role has been revoked but record is kept for audit
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
