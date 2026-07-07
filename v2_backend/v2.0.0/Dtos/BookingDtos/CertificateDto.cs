namespace Vaxtrack.Dtos.BookingDtos
{
    // Public, unauthenticated view of a completed vaccination — deliberately excludes anything
    // beyond what a real vaccination certificate would show (no email/phone/address).
    public class CertificateDto
    {
        public string BookingId { get; set; } = "";
        public string BeneficiaryName { get; set; } = "";
        public int BeneficiaryAge { get; set; }
        public string BeneficiaryGender { get; set; } = "";
        public string Dose1HospitalName { get; set; } = "";
        public DateTime? Dose1CompletedDate { get; set; }
        public string Dose2HospitalName { get; set; } = "";
        public DateTime? Dose2CompletedDate { get; set; }
        public DateTime? VaccinationCompletedDate { get; set; }
    }
}
