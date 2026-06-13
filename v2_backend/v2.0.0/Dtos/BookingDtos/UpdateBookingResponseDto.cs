

namespace Vaxtrack.Dtos.BookingDtos
{
    public class UpdateBookingResponseDto
    {
        public string BookingId {get; set;} = "";
        public string UserUid {get; set;} = "";
        public DateTime Dose1RequestedDateTime {get; set;}
        public int Dose1SlotNumber {get; set;}
        public string Dose1HospitalUid {get; set;} = "";
        public bool IsDose1Completed {get; set;}
        public DateTime Dose1CompletedDateTime {get; set;}
        public DateTime Dose2RequestedDateTime {get; set;}
        public string Dose2HospitalUid {get; set;} = "";
        public bool IsDose2Completed {get; set;}
        public DateTime Dose2CompletedDateTime {get; set;}
        public bool IsVaccinationCompleted {get; set;}
        public DateTime VaccinationCompletedDateTime {get; set;}
        public bool IsD1RequestCanceled {get; set;} = false;
        public bool IsD2RequestCanceled {get; set;} = false;
    }
}