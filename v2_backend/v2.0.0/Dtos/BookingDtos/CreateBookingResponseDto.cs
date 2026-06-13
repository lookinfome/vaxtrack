

namespace Vaxtrack.Dtos.BookingDtos
{
    public class CreateBookingResponseDto
    {
        public string BookingId {get; set;} = "";
        public string UserUid {get; set;} = "";
        public DateTime Dose1RequestedDateTime {get; set;}
        public int Dose1SlotNumber {get; set;}
        public string Dose1HospitalUid {get; set;} = "";   
    }
}