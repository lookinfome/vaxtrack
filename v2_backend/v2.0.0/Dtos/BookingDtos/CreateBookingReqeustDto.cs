

using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.BookingDtos
{
    public class CreateBookingRequestDto
    {
        [Required(ErrorMessage = "user id is required")]
        public string UserUid {get; set;} = "";

        [Required(ErrorMessage = "dose 1 requested date is required")]
        public DateTime Dose1RequestedDateTime {get; set;}

        [Required(ErrorMessage = "dose 1 hospital id is required")]
        public string Dose1HospitalUid {get; set;} = "";
    }
}