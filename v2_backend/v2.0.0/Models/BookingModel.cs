

using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Models
{
    public class BookingModel
    {
        [Key]
        public string BookingId {get; set;} = "";
        public string BookingUid {get; set;} = "";
        public string UserUid {get; set;} = "";

        // for dose 1
        public DateTime Dose1RequestedDateTime {get; set;}
        public int Dose1SlotNumber {get; set;}
        public string Dose1HospitalUid {get; set;} = "";
        public bool IsDose1Completed {get; set;}
        public DateTime Dose1CompletedDateTime {get; set;}
        public bool IsD1RequestCanceled {get; set;} = false;

        // for dose 2
        public DateTime Dose2RequestedDateTime {get; set;}
        public int Dose2SlotNumber {get; set;}
        public string Dose2HospitalUid {get; set;} = "";
        public bool IsDose2Completed {get; set;}
        public DateTime Dose2CompletedDateTime {get; set;}
        public bool IsD2RequestCanceled {get; set;} = false;

        // for vaccination status
        public bool IsVaccinationCompleted {get; set;}
        public DateTime VaccinationCompletedDateTime {get; set;}

        // for record history
        public DateTime CreatedAt {get; set;}
        public DateTime ModifiedAt {get; set;}
        public DateTime RemovedAt {get; set;}
        public bool IsDeleted {get; set;} = false;
    
    }
}