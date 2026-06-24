
using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.BookingDtos
{
    public class BookDose2RequestDto
    {
        [Required(ErrorMessage = "booking id is required")]
        public string BookingId {get; set;} = "";

        [Required(ErrorMessage = "user id is required")]
        public string UserUid {get; set;} = "";

        [Required(ErrorMessage = "dose 2 hospital id is required")]
        public string Dose2HospitalUid {get; set;} = "";

        [Required(ErrorMessage = "dose 2 slot number is required")]
        public int Dose2SlotNumber {get; set;}        
        
        [Required(ErrorMessage = "dose 2 requested date is required")]
        public DateTime Dose2RequestedDateTime {get; set;}
        
    }
}