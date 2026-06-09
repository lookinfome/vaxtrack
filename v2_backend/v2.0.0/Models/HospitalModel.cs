
using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Models
{
    public class HospitalModel
    {
        [Key]
        public string HospitalId { get; set; } = "";
        public string HospitalUid { get; set; } = "";
        public string HospitalName { get; set; } = "";
        public string HospitalAddress { get; set; } = "";
        public string HospitalPinCode { get; set; } = "";
        public string HospitalPhoneNumber { get; set; } = "";
        public string HospitalEmail { get; set; } = "";
        public int TotalSlots { get; set; }
        public int SlotsAvailable { get; set; }
        public DateTime RegisteredDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime RemovedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}