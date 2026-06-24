using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Models
{
    public class UserModel
    {
        // ── identity ─────────────────────────────────────────────────────────────
        [Key]
        public string UserId { get; set; } = "";       // human-readable primary key
        public string UserUid { get; set; } = "";      // system GUID, used as FK in bookings and role mappings

        // ── profile ──────────────────────────────────────────────────────────────
        public string UserName { get; set; } = "";
        public DateTime UserBirthdate { get; set; }
        public int UserAge { get; set; }
        public string UserGender { get; set; } = "";
        public string UserPhone { get; set; } = "";
        public string UserAddress { get; set; } = "";
        public string UserPinCode { get; set; } = "";
        public string ProfilePicturePath { get; set; } = "";

        // ── access ───────────────────────────────────────────────────────────────
        // true = platform admin; false = regular user (default)
        // Additional scoped roles (e.g. hospital-admin) are stored in UserRoleMappings
        public bool UserRole { get; set; } = false;

        // ── record state ─────────────────────────────────────────────────────────
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }       // null until first profile update
        public DateTime? DeletedAt { get; set; }       // null until soft-deleted
    }
}
