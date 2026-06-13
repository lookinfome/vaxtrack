

using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.BookingDtos
{
    public class CreateBookingRequestDto
    {
        [Required(ErrorMessage = "booking id is required")]
        public string BookingId {get; set;} = "";

        [Required(ErrorMessage = "user id is required")]
        public string UserUid {get; set;} = "";

        [Required(ErrorMessage = "dose 1 requested date is required")]
        public DateTime Dose1RequestedDateTime {get; set;}

        [Required(ErrorMessage = "dose 1 slot number is required")]
        public int Dose1SlotNumber {get; set;}

        [Required(ErrorMessage = "dose 1 hospital id is required")]
        public string Dose1HospitalUid {get; set;} = "";
    }
}