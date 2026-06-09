using System.ComponentModel.DataAnnotations;

namespace Vaxtrack.Dtos.HospitalDtos
{
    public class CreateHospitalRequestDto
    {
        [Required(ErrorMessage = "Hospital name is required.")]
        public string HospitalName { get; set; } = "";
    }
}