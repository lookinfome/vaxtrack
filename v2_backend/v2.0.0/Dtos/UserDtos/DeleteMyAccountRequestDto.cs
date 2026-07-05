namespace Vaxtrack.Dtos.UserDtos
{
    public class DeleteMyAccountRequestDto
    {
        public string Password { get; set; } = "";
        public string? Reason { get; set; }
    }
}
