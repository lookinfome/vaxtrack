

namespace Vaxtrack.Dtos.HospitalDtos
{
    public class CreateHospitalResponseDto
    {
        public string HospitalId { get; set; } = "";
        public string HospitalName { get; set; } = "";
        public string HospitalAddress { get; set; } = "";
        public string HospitalPhoneNumber { get; set; } = "";
        public string HospitalEmail { get; set; } = "";
        public int TotalSlots { get; set; }
        public int SlotsAvailable { get; set; }
        public DateTime RegisteredDate { get; set; }
        public string HospitalPinCode { get; set; } = "";
    }
}