namespace Vaxtrack.Dtos.HospitalDtos
{
    // The hospital-admin re-enters their own login password to confirm a platform admin's
    // unregister request — a second-party safety check before the hospital is permanently removed.
    public class AuthorizeUnregisterRequestDto
    {
        public string Password { get; set; } = "";
        public string? Comment { get; set; }
    }
}
