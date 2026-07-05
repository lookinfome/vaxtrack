using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vaxtrack.Models
{
    // Backs both self-service request types raised from the Support page:
    // account reactivation (submitted anonymously by a disabled user) and
    // hospital-admin applications (submitted by a logged-in regular user).
    public class UserRequestModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string UserUid { get; set; } = "";

        // "AccountReactivation" | "HospitalAdminApplication"
        public string RequestType { get; set; } = "";

        // Set only for HospitalAdminApplication — the hospital being applied for
        public string? TargetHospitalId { get; set; }

        // "Pending" | "Approved" | "Rejected"
        public string Status { get; set; } = "Pending";

        public string? UserComment { get; set; }
        public string? AdminComment { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedByUserUid { get; set; }
    }
}
