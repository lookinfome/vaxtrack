namespace Vaxtrack.Dtos.AuthDtos
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public string UserId { get; set; } = "";
        public string UserUid { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool UserRole { get; set; } = false;
    }
}
