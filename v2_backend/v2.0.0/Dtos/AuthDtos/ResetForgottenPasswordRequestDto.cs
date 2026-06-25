namespace Vaxtrack.Dtos.AuthDtos
{
    public class ResetForgottenPasswordRequestDto
    {
        public string ResetToken { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
