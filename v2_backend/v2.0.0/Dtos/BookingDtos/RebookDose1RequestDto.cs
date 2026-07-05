using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.BookingDtos
{
    public class RebookDose1RequestDto
    {
        [Required(ErrorMessage = "booking id is required")]
        public string BookingId { get; set; } = "";

        [Required(ErrorMessage = "user uid is required")]
        public string UserUid { get; set; } = "";

        [Required(ErrorMessage = "new hospital uid is required")]
        public string NewHospitalUid { get; set; } = "";

        [Required(ErrorMessage = "new requested date is required")]
        public DateTime NewRequestedDateTime { get; set; }
    }
}
