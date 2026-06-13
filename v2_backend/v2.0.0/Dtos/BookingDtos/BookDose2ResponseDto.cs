

namespace Vaxtrack.Dtos.BookingDtos
{
    public class BookDose2ResponseDto
    {
        public string BookingId {get; set;} = "";
        public string UserUid {get; set;} = "";
        public DateTime Dose2RequestedDateTime {get; set;}
        public string Dose2HospitalUid {get; set;} = "";
        public bool IsDose2Completed {get; set;}
    }
}