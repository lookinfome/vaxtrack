
using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.BookingDtos
{
    public class BookDose2RequestDto
    {
        [Required(ErrorMessage = "booking id is reuqired")]
        public string BookingId {get; set;} = "";
        
        [Required(ErrorMessage = "user id is required")]
        public string UserUid {get; set;} = "";
        public DateTime Dose2RequestedDateTime {get; set;}
        public string Dose2HospitalUid {get; set;} = "";
        public bool IsDose2Completed {get; set;}
        
    }
}