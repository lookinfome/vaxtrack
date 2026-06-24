using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.BookingDtos
{
    public class UpdateBookingRequestDto
    {
        [Required(ErrorMessage = "booking id is required")]
        public string BookingId { get; set; } = "";

        [Required(ErrorMessage = "user id is required")]
        public string UserUid { get; set; } = "";

        // dose 1 fields
        public DateTime Dose1RequestedDateTime { get; set; }
        public int Dose1SlotNumber { get; set; }
        public string Dose1HospitalUid { get; set; } = "";
        public bool IsDose1Completed { get; set; }
        public DateTime? Dose1CompletedDateTime { get; set; }
        public bool IsD1RequestCanceled { get; set; }

        // dose 2 fields — nullable because dose 2 may not have been booked yet
        public DateTime? Dose2RequestedDateTime { get; set; }
        public int Dose2SlotNumber { get; set; }
        public string Dose2HospitalUid { get; set; } = "";
        public bool IsDose2Completed { get; set; }
        public DateTime? Dose2CompletedDateTime { get; set; }
        public bool IsD2RequestCanceled { get; set; }

        // vaccination status
        public bool IsVaccinationCompleted { get; set; }
        public DateTime? VaccinationCompletedDateTime { get; set; }
    }
}
