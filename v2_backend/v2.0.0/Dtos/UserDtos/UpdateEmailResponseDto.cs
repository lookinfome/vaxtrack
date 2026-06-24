namespace Vaxtrack.Dtos.UserDtos
{
    public class UpdateEmailResponseDto
    {
        public string UserId { get; set; } = "";
        public string NewEmail { get; set; } = "";
        public DateTime? UpdatedAt { get; set; }
    }
}
