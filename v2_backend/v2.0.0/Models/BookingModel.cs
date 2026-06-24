using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Models
{
    public class BookingModel
    {
        // ── identity ─────────────────────────────────────────────────────────────
        [Key]
        public string BookingId { get; set; } = "";    // human-readable primary key
        public string BookingUid { get; set; } = "";   // system GUID
        public string UserUid { get; set; } = "";      // FK → UserModel.UserUid

        // ── dose 1 ───────────────────────────────────────────────────────────────
        public DateTime Dose1RequestedDateTime { get; set; }   // always set at booking creation
        public int Dose1SlotNumber { get; set; }
        public string Dose1HospitalUid { get; set; } = "";
        public bool IsDose1Completed { get; set; } = false;
        public DateTime? Dose1CompletedDateTime { get; set; }  // null until approved
        public bool IsD1RequestCanceled { get; set; } = false;

        // ── dose 2 ───────────────────────────────────────────────────────────────
        public DateTime? Dose2RequestedDateTime { get; set; }  // null until dose 2 is booked
        public int Dose2SlotNumber { get; set; }
        public string Dose2HospitalUid { get; set; } = "";
        public bool IsDose2Completed { get; set; } = false;
        public DateTime? Dose2CompletedDateTime { get; set; }  // null until approved
        public bool IsD2RequestCanceled { get; set; } = false;

        // ── vaccination status ───────────────────────────────────────────────────
        public bool IsVaccinationCompleted { get; set; } = false;
        public DateTime? VaccinationCompletedDateTime { get; set; }  // null until both doses done

        // ── record state ─────────────────────────────────────────────────────────
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime? RemovedAt { get; set; }       // null until soft-deleted
    }
}
