namespace Vaxtrack.Dtos.BookingDtos
{
    public class BookingProfileDataDto
    {
        public string BookingId { get; set; } = "";
        public string UserUid { get; set; } = "";

        // dose 1 — always set at booking creation
        public DateTime Dose1RequestedDateTime { get; set; }
        public int Dose1SlotNumber { get; set; }
        public string Dose1HospitalUid { get; set; } = "";
        public bool IsDose1Completed { get; set; }
        public DateTime? Dose1CompletedDateTime { get; set; }  // null until approved
        public bool IsD1RequestCanceled { get; set; }
        public bool IsD1RejectedByAdmin { get; set; }

        // dose 2 — null until dose 2 is booked
        public DateTime? Dose2RequestedDateTime { get; set; }
        public int Dose2SlotNumber { get; set; }
        public string Dose2HospitalUid { get; set; } = "";
        public bool IsDose2Completed { get; set; }
        public DateTime? Dose2CompletedDateTime { get; set; }  // null until approved
        public bool IsD2RequestCanceled { get; set; }
        public bool IsD2RejectedByAdmin { get; set; }

        // vaccination status
        public bool IsVaccinationCompleted { get; set; }
        public DateTime? VaccinationCompletedDateTime { get; set; }  // null until complete

        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
