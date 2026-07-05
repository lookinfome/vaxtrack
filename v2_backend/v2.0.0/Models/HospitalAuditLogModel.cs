using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vaxtrack.Models
{
    public class HospitalAuditLogModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string HospitalId { get; set; } = "";   // logical FK -> HospitalModel.HospitalId

        // "Disabled" | "ReactivationRequested" | "ReactivationApproved" | "ReactivationRejected" |
        // "UnregisterRequested" | "UnregisterWithdrawn" | "UnregisterDeclined" | "Unregistered"
        public string ActionType { get; set; } = "";

        public string ActorUserUid { get; set; } = "";
        public string ActorRole { get; set; } = "";     // "admin" | "hospital-admin"
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
