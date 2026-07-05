namespace Vaxtrack.Dtos.AuthDtos
{
    public class RequestAccountReactivationRequestDto
    {
        public string Email { get; set; } = "";
        public string? Reason { get; set; }
    }
}
