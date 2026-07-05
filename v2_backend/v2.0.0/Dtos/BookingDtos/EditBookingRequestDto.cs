using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.BookingDtos
{
    public class EditBookingRequestDto
    {
        [Required(ErrorMessage = "booking id is required")]
        public string BookingId { get; set; } = "";

        [Required(ErrorMessage = "user uid is required")]
        public string UserUid { get; set; } = "";

        [Required(ErrorMessage = "dose number is required")]
        [Range(1, 2, ErrorMessage = "dose number must be 1 or 2")]
        public int DoseNumber { get; set; }

        [Required(ErrorMessage = "new hospital uid is required")]
        public string NewHospitalUid { get; set; } = "";

        [Required(ErrorMessage = "new requested date is required")]
        public DateTime NewRequestedDateTime { get; set; }
    }
}
