namespace Vaxtrack.Dtos.AuthDtos
{
    public class ForgotPasswordResponseDto
    {
        // Always shown — same message regardless of whether the email is registered (prevents enumeration)
        public string Message { get; set; } = "";

        // Returned here only because there is no email service yet.
        // In production: email the token, set both fields to null in the response.
        public string? ResetToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
