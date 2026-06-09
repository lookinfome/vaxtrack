
using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.HospitalDtos
{
    public class UpdateHospitalRequestDto
    {
        [Required(ErrorMessage = "Hospital Id is required")]
        public string HospitalId { get; set; } = "";
        public string HospitalAddress { get; set; } = "";
        public string HospitalPinCode { get; set; } = "";
        public string HospitalPhoneNumber { get; set; } = "";
        public string HospitalEmail { get; set; } = "";
    }
}