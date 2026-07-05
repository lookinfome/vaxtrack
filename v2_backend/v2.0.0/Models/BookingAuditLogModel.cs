using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vaxtrack.Models
{
    public class BookingAuditLogModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string BookingId { get; set; } = "";    // logical FK -> BookingModel.BookingId
        public int DoseNumber { get; set; }             // 1 or 2, or 0 for booking-level actions

        // "Dose1Booked" | "Dose2Booked" | "Approved" | "Rejected" | "Cancelled" | "Rebooked" | "Edited"
        public string ActionType { get; set; } = "";

        public string ActorUserUid { get; set; } = "";
        public string ActorRole { get; set; } = "";     // "user" | "hospital-admin" | "admin"
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
