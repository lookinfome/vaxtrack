using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Models
{
    public class HospitalModel
    {
        // ── identity ─────────────────────────────────────────────────────────────
        [Key]
        public string HospitalId { get; set; } = "";   // human-readable primary key
        public string HospitalUid { get; set; } = "";  // system GUID

        // ── profile ──────────────────────────────────────────────────────────────
        public string HospitalName { get; set; } = "";
        public string HospitalAddress { get; set; } = "";
        public string HospitalPinCode { get; set; } = "";
        public string HospitalPhoneNumber { get; set; } = "";
        public string HospitalEmail { get; set; } = "";

        // ── capacity ─────────────────────────────────────────────────────────────
        public int TotalSlots { get; set; }
        public int SlotsAvailable { get; set; }

        // ── record state ─────────────────────────────────────────────────────────
        public bool IsDeleted { get; set; } = false;
        public DateTime RegisteredDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? RemovedDate { get; set; }     // null until soft-deleted
    }
}
