namespace Vaxtrack.Dtos.BookingDtos
{
    public class BookDose2ResponseDto
    {
        public string BookingId { get; set; } = "";
        public string UserUid { get; set; } = "";
        public string Dose2HospitalUid { get; set; } = "";
        public int Dose2SlotNumber { get; set; }
        public DateTime? Dose2RequestedDateTime { get; set; }  // null until dose 2 is booked
        public bool IsDose2Completed { get; set; }
    }
}
